namespace Script.Map.Provider
{
    using Script.Asset.Data;
    using Script.Map.Data;
    using Script.Map.Utility;
    using System.Collections.Generic;
    using Unity.Collections; // Native Container 사용을 위해 추가
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    /// <summary>
    /// [Framework] Provider: 맵 데이터 및 에셋(순수 데이터, 메쉬 등)의 공급과 메모리를 관리합니다.
    /// 값(Value) 중심의 런타임 데이터 제공을 위해 Native 자료구조의 생명주기도 전담합니다.
    /// </summary>
    public class MapRepoProvider : System.IDisposable
    {
        // --- 런타임 데이터 캐시 ---
        private readonly Dictionary<int, List<MapGridLayerData>> _gridLayerDict; // 그리드별 레이어 정보
        private readonly Dictionary<long, MapTileData> _tileDict;               // 전체 타일 네비게이션 데이터
        private readonly Dictionary<int3, long> _posToID;                       // 좌표 기반 ID 역추적

        // --- 런타임 패스파인딩을 위한 네이티브 맵 캐시 (값 중심의 자료구조) ---
        // [MapSampling] 규칙에 따라 NaviMask와 LinkMask를 담습니다.
        private NativeHashMap<long, (long, long)> _nativeMap;

        // --- Addressables 메모리 핸들 캐시 ---
        // 특정 그리드를 언로드할 때 관련 메쉬 메모리를 정확히 해제하기 위해 보관합니다.
        private readonly Dictionary<int, List<AsyncOperationHandle<Mesh>>> _loadedMeshHandles;

        public Dictionary<long, MapTileData> TileDic => _tileDict;

        /// <summary>
        /// Manager 등에서 길찾기 유틸리티를 호출할 때 넘겨줄 수 있도록 노출합니다.
        /// </summary>
        public NativeHashMap<long, (long, long)> NativeMap => _nativeMap;

        public MapRepoProvider()
        {
            _gridLayerDict = new Dictionary<int, List<MapGridLayerData>>();
            _tileDict = new Dictionary<long, MapTileData>();
            _posToID = new Dictionary<int3, long>();
            _loadedMeshHandles = new Dictionary<int, List<AsyncOperationHandle<Mesh>>>();
            
            // 초기화 시 빈 컨테이너 생성 (Allocator.Persistent를 사용하여 생명주기를 수동으로 관리)
            _nativeMap = new NativeHashMap<long, (long, long)>(0, Allocator.Persistent);
        }

        /// <summary>
        /// 특정 주소의 맵 네비게이션 데이터를 비동기로 로드하고 초기화합니다.
        /// </summary>
        public async Awaitable LoadFromAddressableAsync(string gridAddress)
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(gridAddress);
            TextAsset ta = await handle.Task; // Unity 6000의 Awaitable은 Task 대기를 지원합니다.
            
            if (null == ta)
            {
                Debug.LogError($"MapRepoProvider: Addressable not found: {gridAddress}");
                return;
            }

            try
            {
                // MessagePack을 이용한 역직렬화 수행
                MapGridData grid = SerializeUtil.Deserialize<MapGridData>(ta.bytes);
                Initialize(grid);

                // 사용이 끝난 TextAsset 핸들은 즉시 해제
                Addressables.Release(handle);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"MapRepoProvider Serialization Error: {e.Message}");
            }
        }

        /// <summary>
        /// 역직렬화된 데이터를 Provider 내부 딕셔너리에 최적화된 구조로 재배치합니다.
        /// 런타임 패스파인딩을 위한 네이티브 데이터도 여기서 동기화합니다.
        /// </summary>
        private void Initialize(MapGridData grid)
        {
            int gKey = grid.Key;
            _gridLayerDict.TryAdd(gKey, grid.layerMeshAssets);

            // Capacity를 미리 늘려두면 메모리 재할당 오버헤드를 줄일 수 있습니다.
            if (_nativeMap.Capacity < _tileDict.Count + grid.NaviTileDict.Count)
            {
                _nativeMap.Capacity = _tileDict.Count + grid.NaviTileDict.Count;
            }

            foreach (var tileKV in grid.NaviTileDict)
            {
                int tKey = tileKV.Key;
                MapTileData tile = tileKV.Value;

                // 그리드 키와 타일 키를 조합하여 유니크한 long ID 생성
                MapCoordUtil.ComputeID(gKey, tKey, out long id);
                _tileDict[id] = tile;

                // 정수형 절대 좌표를 키로 사용하여 타일 식별 속도 최적화
                MapCoordUtil.ComputeWorldPositionInt(id, out int3 absPivot);
                _posToID.TryAdd(absPivot, id);

                // [MapSampling] 네이티브 맵에도 데이터 동기화
                _nativeMap.TryAdd(id, (tile.NaviMask, tile.LinkMask));
            }
        }

        /// <summary>
        /// [중요] 특정 그리드의 모든 레이어 메쉬를 병렬로 로드합니다.
        /// Awaitable.WhenAll 대신 모든 로드를 먼저 실행(Trigger)하고 결과를 순차 대기하는 방식을 사용합니다.
        /// </summary>
        public async Awaitable<Dictionary<int, List<Mesh>>> LoadGridMeshesAsync(int gridKey)
        {
            if (!_gridLayerDict.TryGetValue(gridKey, out List<MapGridLayerData> layers)) return null;

            var result = new Dictionary<int, List<Mesh>>();
            var loadingHandles = new List<(int layer, AsyncOperationHandle<Mesh> handle)>();
            
            if (!_loadedMeshHandles.ContainsKey(gridKey))
                _loadedMeshHandles[gridKey] = new List<AsyncOperationHandle<Mesh>>();

            // 1. 병렬 로드 트리거: 모든 에셋 로드 요청을 동시에 날려 OS 레벨의 병렬 로딩을 유도합니다.
            foreach (var layerData in layers)
            {
                int layerIdx = layerData.layer;
                result[layerIdx] = new List<Mesh>();

                foreach (string address in layerData.assets)
                {
                    var handle = Addressables.LoadAssetAsync<Mesh>(address);
                    loadingHandles.Add((layerIdx, handle));
                    _loadedMeshHandles[gridKey].Add(handle); // 추후 해제를 위해 기록
                }
            }

            // 2. 결과 수집: 로드된 핸들의 결과를 순차적으로 await 하여 결과 리스트에 담습니다.
            foreach (var item in loadingHandles)
            {
                Mesh loadedMesh = await item.handle.Task;
                if (item.handle.Status == AsyncOperationStatus.Succeeded)
                    result[item.layer].Add(loadedMesh);
            }

            return result;
        }

        /// <summary>
        /// 특정 그리드와 관련된 모든 메쉬 핸들을 해제하여 Addressables 메모리를 정리합니다.
        /// </summary>
        public void ReleaseGridMeshes(int gridKey)
        {
            if (_loadedMeshHandles.TryGetValue(gridKey, out var handles))
            {
                foreach (var h in handles)
                {
                    if (h.IsValid()) Addressables.Release(h);
                }
                _loadedMeshHandles.Remove(gridKey);
            }
        }

        /// <summary>
        /// Provider가 파괴되거나 씬 전환 등으로 생명주기가 끝날 때 할당된 네이티브 메모리를 반드시 해제합니다.
        /// </summary>
        public void Dispose()
        {
            if (_nativeMap.IsCreated)
            {
                _nativeMap.Dispose();
            }
            
            // 안전을 위해 로드된 메쉬 핸들도 여기서 일괄 정리합니다.
            foreach (var kv in _loadedMeshHandles)
            {
                foreach (var h in kv.Value)
                {
                    if (h.IsValid()) Addressables.Release(h);
                }
            }
            _loadedMeshHandles.Clear();
        }
    }
}