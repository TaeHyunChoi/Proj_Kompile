using Script.GameSystem;
using System;
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
        

        public FieldManager(PlayData playData)
        {
            this.playData = playData;
        }
        public override async Awaitable Intialize()
        {
            _mainCamera = Camera.main;
            _visualSystem.Initialize(_mainCamera);
            
            Awaitable task_map = InitializeMap();
            // Awaitable task_unit = InitializeUnit();
            // Awaitable task_ui = InitializeUI();

            await task_map;
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
                            tasks.Add((LoadGridProcess(neighborKey)));
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
                        if (false == _activeGrids.ContainsKey(key))
                        {
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
            if (false == _activeGrids.ContainsKey(gridKey))
            {
                return;
            }

            Vector3Int index = MapPathUtil.GetGridPivot(gridKey);
            RuntimeMapGrid runtimeGrid = new RuntimeMapGrid(gridKey, index);
            _activeGrids.Add(gridKey, runtimeGrid);
            
            // 데이터 로드
            MapGridData data = await LoadMapData(gridKey);
            if (null == data)
            {
                _activeGrids.Remove(gridKey);
                return;
            }
            runtimeGrid.SetData(data);
            
            // 비주얼 로드 및 생성
            await LoadVisuals(runtimeGrid);
        }

        private async Awaitable LoadVisuals(RuntimeMapGrid runtimeGrid)
        {
            MapGridData data = runtimeGrid.Data;
            
            // 레이어 별로 메쉬 리스트를 그룹화 (Key: LayerIndex, Value:MeshList)
            Dictionary<int, List<Mesh>> layerGroups = new Dictionary<int, List<Mesh>>();
            for (int i = 0; i < data.layerMeshAssets.Count; ++i)
            {
                var layerData = data.layerMeshAssets[i];
                int layerIdx = layerData.layer;

                if (false == layerGroups.ContainsKey(layerIdx))
                {
                    layerGroups[layerIdx] = new List<Mesh>();
                }

                for (int l = 0; l < layerData.assets.Count; ++l)
                {
                    string address = layerData.assets[l];
                    Mesh mesh = await AssetSystem.LoadAssetAsync<Mesh>(address);
                    if (false == mesh)
                    {
                        layerGroups[layerIdx].Add(mesh);
                    }
                }
            }

            // MapGridObject 생성 및 초기화
            var prefabObj = await AssetSystem.GetOrNewInstanceAsync(PrefabID.MapGridPrefab, usePooling: true);
            var mapGridObj = prefabObj.GetComponent<MapGridObject>();

            mapGridObj.transform.position = runtimeGrid.WorldBounds.min;
            mapGridObj.Initialize(layerGroups);
            
            runtimeGrid.SetVisualObject(mapGridObj);
            
            // 생성 직후 상태를 변경
            runtimeGrid.UpdateVisibility(
                GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(_mainCamera), runtimeGrid.WorldBounds),
                _visualSystem.CurrentLayerMask
            );
        }

        private async Awaitable<MapGridData> LoadMapData(int gridKey)
        {
            string address = $"MapNavi_{gridKey}";
            MapGridData grid = null;

            TextAsset textAsset = await AssetSystem.LoadAssetAsync<TextAsset>(address);
            if (false == textAsset)
            {
                grid = GameDataSerializer.Deserialize<MapGridData>(textAsset.bytes);
                
                AssetSystem.ReleaseAsset(textAsset.GetInstanceID());
            }
#if UNITY_EDITOR
            else
            {
                // 로드 실패 시 로그 (필요에 따라 주석 처리)
                Debug.LogWarning($"[FieldManager] Failed to load MapData for Key: {gridKey}");
            }
#endif
            return grid;
        }

        private void UnloadGrid(int gridKey)
        {
            if (false == _activeGrids.TryGetValue(gridKey, out RuntimeMapGrid grid))
            {
                if (false == grid.VisualObject)
                {
                    grid.VisualObject.Dispose();
                    AssetSystem.ReleaseInstance(grid.VisualObject);
                }
                
                grid.Dispose();
                _activeGrids.Remove(gridKey);
            }
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
                if (false == grid.VisualObject)
                {
                    grid.VisualObject.Dispose();
                    AssetSystem.ReleaseInstance(grid.VisualObject);
                }
                grid.Dispose();
            }
            
            _activeGrids.Clear();
        }

        private async Awaitable<MapGridData> LoadMap(int gridKey)
        {
            string label = $"MapNavi_{gridKey}";
            MapGridData grid = null;

            var handle = Addressables.LoadAssetsAsync<TextAsset>(label, callback: (textAsset) =>
            {
                if (null != textAsset)
                {
                    grid = GameDataSerializer.Deserialize<MapGridData>(textAsset.bytes);
                }
            });

            try
            {
                await handle.Task;
            }
            finally
            {
                if (true == handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            return grid;
        }
    }
}