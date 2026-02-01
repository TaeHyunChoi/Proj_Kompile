using Script.GameSystem;
using System;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace Script.GamePlay
{
    using Script.Asset;
    using Script.Data;
    using Script.Map;
    using Script.GameSystem.Utility;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    public class FieldManager : ManagerBase
    {
        private PlayData playData;
        private Camera _mainCamera;
        
        private MapVisualSystem _visualSystem = new  MapVisualSystem();
        private Dictionary<int, RuntimeMapGrid> _activeGrids = new Dictionary<int, RuntimeMapGrid>();
        private int _lastCenterGridKey = int.MinValue;

        // 비동기 로딩 중 중복 요청 방지, 상태 추적을 위한 set
        private HashSet<int> _loadingGrids = new HashSet<int>();

        public FieldManager(PlayData playData)
        {
            this.playData = playData;
        }
        public override async Awaitable Intialize()
        {
            _mainCamera = Camera.main;
            _visualSystem.Initialize(_mainCamera);
            
            Awaitable task_map = InitializeMap();
            await task_map;
            
            // Awaitable task_unit = InitializeUnit();
            // Awaitable task_ui = InitializeUI();
            
            // await task_unit;
            // await task_ui;
        }

        public override bool OnUpdate()
        {
            if(false == _mainCamera)
            {
                return false;
            }

            CheckAndLoadSurroundingGrids();
            _visualSystem.UpdateCulling(_activeGrids);
            
            return true;
        }
        
        // --- 맵 로딩 프로세스 ---
        private async Awaitable InitializeMap()
        {
            int centerKey = MapPathUtil.ComputeGridKey(playData.Position);
            _lastCenterGridKey = centerKey;
            
            await LoadSurroundingGrids(centerKey);
        }

        private async Awaitable LoadSurroundingGrids(int centerKey)
        {
            List<Awaitable> tasks = new List<Awaitable>();
            
                // 5*5*5 범위 로드
                for (int x = -2; x <= 2; ++x)
                {
                    for (int y = -2; y <= 2; ++y)
                    {
                        for (int z = -2; z <= 2; ++z)
                        {
                            int neighborKey = MapPathUtil.ComputeGridKey(centerKey, new int3(x, y, z));
                            tasks.Add(LoadGridProcess(neighborKey));
                        }
                    }
                }

                foreach (Awaitable task in tasks)
                {
                    await task;
                }
        }

        private void CheckAndLoadSurroundingGrids()
        {
            if (null == playData)
            {
                return;
            }
            
            int currentKey = MapPathUtil.ComputeGridKey(playData.Position);
            if (_lastCenterGridKey == currentKey)
            {
                // 이전과 동일하므로 Grid 변경 없음
                return;
            }

            _lastCenterGridKey = currentKey;
            
            HashSet<int> neededKeys = new  HashSet<int>();
            for (int x = -2; x <= 2; ++x)
            {
                for (int y = -2; y <= 2; ++y)
                {
                    for (int z = -2; z <= 2; ++z)
                    {
                        int key = MapPathUtil.ComputeGridKey(currentKey, new int3(x, y, z));
                        neededKeys.Add(key);
                        
                        // fire-and-forget (update 멈춤 방지)
                        if (false == _activeGrids.ContainsKey(key)
                            && false == _loadingGrids.Contains(key))
                        {
                            // fire-and-forget: 비동기 실행하되 예외는 내부에서 처리
                            LoadGridProcess(key);
                        }
                    }
                }
            }
            
            // 멀어진 grid 언로드
            List<int> keyToRemove = new List<int>();
            foreach (int key in _activeGrids.Keys)
            {
                if (false == neededKeys.Contains(key))
                {
                    keyToRemove.Add(key);
                }
            }

            foreach (int key in keyToRemove)
            {
                UnloadGrid(key);
            }
        }

        private async Awaitable LoadGridProcess(int gridKey)
        {
            // 중복 로딩 방지 등록
            if (true == _loadingGrids.Contains(gridKey))
            {
                return;
            }
            _loadingGrids.Add(gridKey);

            try
            {
                // 그리드 데이터 컨테이너 생성, 등록
                Vector3Int index = MapPathUtil.GetGridPivot(gridKey);
                RuntimeMapGrid runtimeMapGrid = new RuntimeMapGrid(gridKey, index);

                // 미리 activeGrids에 등록하여 Unload 대상이 될 수 있게 함 => 상태 관리가 더 명확해진다.
                _activeGrids.Add(gridKey, runtimeMapGrid);

                // 데이터 로드 (비동기 대기)
                MapGridData data = await LoadMapData(gridKey);

                // (race condition) await 하는 동안 이 그리드가 unload가 되어 active 목록에서 사라진다면? 중단
                if (false == _activeGrids.ContainsKey(gridKey))
                {
                    // 이미 UnloadGrid가 호출되어 RuntimeMapGrid가 Dispose 되었을 것
                    // 데이터 날리고 종료
                    return;
                }

                if (null == data)
                {
                    // 데이터 로드 실패 시 정리
                    UnloadGrid(gridKey);
                    return;
                }

                runtimeMapGrid.SetData(data);

                // 비주얼 로드 및 생성 (비동기 대기)
                await LoadVisuals(runtimeMapGrid);

                // (race condition) Visual Load 중 Unload 되었을 수 있음
                if (false == _activeGrids.ContainsKey(gridKey))
                {
                    // VisualObject가 생성되었다면 UnloadGrid 로직에 의해 정리가 안 됐을 수 있으므로 안전하게 정리
                    if (true == runtimeMapGrid.VisualObject)
                    {
                        AssetSystem.ReleaseInstance(runtimeMapGrid.VisualObject);
                    }
                }
            }
            catch (Exception e)
            {
                UnloadGrid(gridKey); // 에러 발생하면 안전하게 해제
            }
            finally
            {
                // 로딩 종료 표시
                _loadingGrids.Remove(gridKey);
            }
        }

        private async Awaitable LoadVisuals(RuntimeMapGrid runtimeGrid)
        {
            MapGridData data = runtimeGrid.Data;
            if (null == data.layerMeshAssets)
            {
                return;
            }
            
            // 레이어 별로 메쉬 리스트를 그룹화
            Dictionary<int, List<Mesh>> layerGroups = new Dictionary<int, List<Mesh>>();
            
            // (최적화) 병렬 로딩을 위한 Task 리스트
            List<Task<(int layerIdx, Mesh mesh)>> loadTasks = new List<Task<(int, Mesh)>>();

            for (int i = 0; i < data.layerMeshAssets.Count; ++i)
            {
                var layerData = data.layerMeshAssets[i];
                int layerIdx = layerData.layer;

                if (false == layerGroups.ContainsKey(layerIdx))
                {
                    layerGroups[layerIdx] =  new List<Mesh>();
                }

                for (int l = 0; l < layerData.assets.Count; ++l)
                {
                    string address = layerData.assets[l];
                    
                    // 인덱스와 결과를 함께 캡쳐
                    loadTasks.Add(LoadMeshWithLayerInfo(address, layerIdx));
                }
            }
            
            // 모든 메쉬가 로드될 때까지 병렬 대기 (Serial Await보다 훨씬 빠름)
            var results = await Task.WhenAll(loadTasks);
            
            // 결과 처리
            foreach (var res in results)
            {
                if (false != res.mesh)
                {
                    layerGroups[res.layerIdx].Add(res.mesh);
                }
            }
            
            // MapGridObject 생성 및 초기화
            var prefabObj = await AssetSystem.GetOrNewInstanceAsync(PrefabID.MapGridPrefab, usePooling: true);
            var mapGridObj = prefabObj.GetComponent<MapGridObject>();

            // 초기화
            mapGridObj.transform.position = Vector3.zero;
            mapGridObj.Initialize(layerGroups);
            
            runtimeGrid.SetVisualObject(mapGridObj);
            
            // 생성 직후 상태 업데이트
            runtimeGrid.UpdateVisibility(
                GeometryUtility.TestPlanesAABB(_visualSystem.GetFrustumPlanes(), runtimeGrid.WorldBounds),
                _visualSystem.CurrentLayerMask
            );

            // 병렬 로딩을 위한 헬퍼 함수
            async Task<(int layerIdx, Mesh mesh)> LoadMeshWithLayerInfo(string address, int layerIdx)
            {
                Mesh mesh = await AssetSystem.LoadAssetAsync<Mesh>(address);
                return (layerIdx, mesh);
            }
        }

        private async Awaitable<MapGridData> LoadMapData(int gridKey)
        {
            string address = $"MapNavi_{gridKey}";

            // 키 존재 여부를 먼저 확인하여 InvalidKeyException 방지
            // Addressables.LoadResourceLocationsAsync는 키가 없으면 빈 리스트를 반환하며 예외를 던지지 않음
            var locationHandle = Addressables.LoadResourceLocationsAsync(address);
            var locations = await locationHandle.Task;

            bool isKeyValid = locations != null && locations.Count > 0;
            Addressables.Release(locationHandle); // 핸들 해제 필수

            if (false == isKeyValid)
            {
                // 키가 없으므로 null 반환 (LoadGridProcess에서 _emptyGrids로 처리됨)
                return null;
            }

            MapGridData grid = null;

            // 키가 유효함을 확인했으므로 안전하게 로드
            TextAsset textAsset = await AssetSystem.LoadAssetAsync<TextAsset>(address);

            if (textAsset != null)
            {
                grid = GameDataSerializer.Deserialize<MapGridData>(textAsset.bytes);
                AssetSystem.ReleaseAsset(textAsset.GetInstanceID());
            }

            return grid;
        }

        private void UnloadGrid(int gridKey)
        {
            if (false == _activeGrids.TryGetValue(gridKey, out RuntimeMapGrid grid))
            {
                return;
            }

            if (true == grid.VisualObject)
            {
                grid.VisualObject.Dispose();
                AssetSystem.ReleaseInstance(grid.VisualObject);
            }

            grid.Dispose();
            _activeGrids.Remove(gridKey);
        }

        // private async Awaitable InitializeUnit()
        // { 
        //
        // }
        // private async Awaitable InitializeUI()
        // { 
        //
        // }

        public override bool OnInputReceive(DataType.InputState inputState)
        {
            return false;
        }


        public override void Dispose()
        {
            foreach (var grid in _activeGrids.Values)
            {
                if (true == grid.VisualObject)
                {
                    grid.VisualObject.Dispose();
                    AssetSystem.ReleaseInstance(grid.VisualObject);
                }
                
                grid.Dispose();
            }
            
            _activeGrids.Clear();
            _loadingGrids.Clear();
        }
    }
}