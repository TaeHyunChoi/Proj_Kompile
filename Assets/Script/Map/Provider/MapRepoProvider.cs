namespace Kompile.Map.Provider
{
    using Kompile.Asset.Utility;
    using Kompile.Map.Data;
    using Kompile.Map.Utility;
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    /// <summary>
    /// [Framework] Provider: 맵 데이터 및 에셋의 공급과 메모리를 관리합니다.
    /// 값(Value) 중심의 런타임 데이터 제공을 위해 Native 자료구조의 생명주기도 전담합니다.
    /// </summary>
    public class MapRepoProvider : System.IDisposable
    {
        // --- 런타임 데이터 캐시 ---
        private readonly Dictionary<int, List<MapGridLayerData>> _gridLayerDict;
        private readonly Dictionary<long, MapTileData> _tileDict;
        private readonly Dictionary<int3, long> _posToID;

        // --- 런타임 패스파인딩을 위한 네이티브 맵 캐시 ---
        private NativeHashMap<long, (long, long)> _nativeMap;

        // --- Addressables 메모리 핸들 캐시 ---
        private readonly Dictionary<int, List<AsyncOperationHandle<Mesh>>> _loadedMeshHandles;

        public Dictionary<long, MapTileData> TileDic => _tileDict;
        public NativeHashMap<long, (long, long)> NativeMap => _nativeMap;

        public MapRepoProvider()
        {
            _gridLayerDict = new Dictionary<int, List<MapGridLayerData>>();
            _tileDict = new Dictionary<long, MapTileData>();
            _posToID = new Dictionary<int3, long>();
            _loadedMeshHandles = new Dictionary<int, List<AsyncOperationHandle<Mesh>>>();

            _nativeMap = new NativeHashMap<long, (long, long)>(0, Allocator.Persistent);
        }

        /// <summary>
        /// [수정] Manager가 문자열 조합을 신경 쓰지 않도록 gridKey만 받아 처리합니다.
        /// 에셋 존재 여부를 먼저 확인하여 에러 로그를 원천 차단하고 성공 여부를 반환합니다.
        /// </summary>
        public async Awaitable<bool> LoadGridDataAsync(int gridKey)
        {
            // 1. 이미 데이터가 로드되어 있다면 중복 로드 생략
            if (_gridLayerDict.ContainsKey(gridKey)) return true;

            string gridAddress = $"MapNavi_{gridKey}";

            // 2. Addressables 카탈로그에 해당 키(주소)가 존재하는지 위치 정보로 검사
            var locationHandle = Addressables.LoadResourceLocationsAsync(gridAddress);
            var locations = await locationHandle.Task;
            bool exists = locations.Count > 0;
            Addressables.Release(locationHandle); // 확인 후 핸들 즉시 해제

            // 맵 데이터가 없는 곳(빈 허공)이라면 조용히 false 반환
            if (!exists) return false;

            // 3. 실제 TextAsset 데이터 로드
            var handle = Addressables.LoadAssetAsync<TextAsset>(gridAddress);
            TextAsset ta = await handle.Task;

            if (ta == null) return false;

            try
            {
                MapGridData grid = SerializeUtil.Deserialize<MapGridData>(ta.bytes);
                Initialize(grid);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"MapRepoProvider Serialization Error: {e.Message}");
                return false;
            }
            finally
            {
                // 로드에 성공하든 예외가 발생하든 TextAsset 메모리는 해제
                Addressables.Release(handle);
            }
        }

        private void Initialize(MapGridData grid)
        {
            int gKey = grid.Key;
            _gridLayerDict.TryAdd(gKey, grid.layerMeshAssets);

            if (_nativeMap.Capacity < _tileDict.Count + grid.NaviTileDict.Count)
            {
                _nativeMap.Capacity = _tileDict.Count + grid.NaviTileDict.Count;
            }

            foreach (var tileKV in grid.NaviTileDict)
            {
                int tKey = tileKV.Key;
                MapTileData tile = tileKV.Value;

                MapCoordUtil.ComputeID(gKey, tKey, out long id);
                _tileDict[id] = tile;

                MapCoordUtil.ComputeWorldPositionInt(id, out int3 absPivot);
                _posToID.TryAdd(absPivot, id);

                _nativeMap.TryAdd(id, (tile.NaviMask, tile.LinkMask));
            }
        }

        public async Awaitable<Dictionary<int, List<Mesh>>> LoadGridMeshesAsync(int gridKey)
        {
            if (!_gridLayerDict.TryGetValue(gridKey, out List<MapGridLayerData> layers))
                return null;

            var result = new Dictionary<int, List<Mesh>>();
            var loadingHandles = new List<(int layer, AsyncOperationHandle<Mesh> handle)>();

            if (!_loadedMeshHandles.ContainsKey(gridKey))
                _loadedMeshHandles[gridKey] = new List<AsyncOperationHandle<Mesh>>();

            foreach (var layerData in layers)
            {
                int layerIdx = layerData.layer;
                result[layerIdx] = new List<Mesh>();

                foreach (string address in layerData.assets)
                {
                    var handle = Addressables.LoadAssetAsync<Mesh>(address);
                    loadingHandles.Add((layerIdx, handle));
                    _loadedMeshHandles[gridKey].Add(handle);
                }
            }

            foreach (var item in loadingHandles)
            {
                Mesh loadedMesh = await item.handle.Task;
                if (item.handle.Status == AsyncOperationStatus.Succeeded)
                    result[item.layer].Add(loadedMesh);
            }

            return result;
        }

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

        public void Dispose()
        {
            if (_nativeMap.IsCreated) _nativeMap.Dispose();

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