namespace Script.Map
{
    using MessagePack;
    using MessagePack.Resolvers;
    using Script.Data;
    using System.Collections.Generic;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    public class MapCacheManager
    {
        private Dictionary<int, List<GridLayerData>> gridLayerDict;
        private Dictionary<long, MapTileData> tileDict;
        private Dictionary<int3, long> posToID;

        public Dictionary<long, MapTileData> TileDic => tileDict;

        public MapCacheManager()
        {
            gridLayerDict = new Dictionary<int, List<GridLayerData>>();
            tileDict = new Dictionary<long, MapTileData>();
        }
        public async Awaitable LoadFromAddressableAsync(string gridAddress)
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(gridAddress);
            TextAsset ta = await handle.Task;
            if (null == ta)
            {
                Debug.LogError($"NodeCacheManager: Addressable not found: {gridAddress}");
                return;
            }

            var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
            try
            {
                MapGridData grid = MessagePackSerializer.Deserialize<MapGridData>(ta.bytes, options);
                Initialize(grid);

                Addressables.Release(handle);
                Debug.Log($"MapCacheManager: Load {grid.NaviTileDict.Keys.Count} nodes from '{gridAddress}'");
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
        private void Initialize(MapGridData grid)
        {
            int count = grid.NaviTileDict.Keys.Count;
            int gKey = grid.Key;
            posToID = new Dictionary<int3, long>(capacity: count);

            // layer info
            gridLayerDict.TryAdd(gKey, grid.layerMeshAssets);

            // tile navi info
            foreach (var tileKV in grid.NaviTileDict)
            {
                int tKey = tileKV.Key;
                MapTileData tile = tileKV.Value;

                long id = MapPathUtil.ComputeID(gKey, tKey);
                if (false == tileDict.TryAdd(id, tile))
                {
                    tileDict[id] = tile;
                }

                int3 absPivot = MapPathUtil.ComputeWorldPositionInt(id);
                posToID.TryAdd(absPivot, id);
            }
        }


        public async Awaitable EditLoadAll()
        {
            // Addressable Label로 모든 MapNavi.bytes 불러오기
            string label = "MapNavi";
            var handle = Addressables.LoadAssetsAsync<TextAsset>(label, callback: (textAsset) =>
            {
                // 칵 파일이 로드될 때마다 실행되는 콜백 (병렬 실행)
                if (null != textAsset)
                {
                    var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
                    MapGridData grid = MessagePackSerializer.Deserialize<MapGridData>(textAsset.bytes, options);
                    Initialize(grid);
                    Debug.Log($"[TEST][Load Baked Map] {textAsset.name}");
                }
            });
            await handle.Task;
            Addressables.Release(handle);
        }
    }
}
