namespace Script.Map.Provider
{
    using Script.Asset.Data;
    using Script.Data;
    using Script.Map.Utility;
    using System.Collections.Generic;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    public class MapCacheProvider
    {
        private Dictionary<int, List<MapGridLayerData>> gridLayerDict;
        private Dictionary<long, MapTileData> tileDict;
        private Dictionary<int3, long> posToID;

        public Dictionary<long, MapTileData> TileDic => tileDict;

        public MapCacheProvider()
        {
            gridLayerDict = new Dictionary<int, List<MapGridLayerData>>();
            tileDict = new Dictionary<long, MapTileData>();
            posToID = new Dictionary<int3, long>();
        }

        public async Awaitable LoadFromAddressableAsync(string gridAddress)
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(gridAddress);
            TextAsset ta = await handle.Task;
            if (null == ta)
            {
                Debug.LogError($"MapCacheProvider: Addressable not found: {gridAddress}");
                return;
            }

            try
            {
                MapGridData grid = SerializeUtil.Deserialize<MapGridData>(ta.bytes);
                Initialize(grid);

                Addressables.Release(handle);
                Debug.Log($"MapCacheProvider: Load {grid.NaviTileDict.Keys.Count} nodes from '{gridAddress}'");
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        private void Initialize(MapGridData grid)
        {
            int gKey = grid.Key;

            // layer info
            gridLayerDict.TryAdd(gKey, grid.layerMeshAssets);

            // tile navi info
            foreach (var tileKV in grid.NaviTileDict)
            {
                int tKey = tileKV.Key;
                MapTileData tile = tileKV.Value;

                MapCoordUtil.ComputeID(gKey, tKey, out long id);
                if (false == tileDict.TryAdd(id, tile))
                {
                    tileDict[id] = tile;
                }

                MapCoordUtil.ComputeWorldPositionInt(id, out int3 absPivot);
                posToID.TryAdd(absPivot, id);
            }
        }

        public async Awaitable EditLoadAll()
        {
            string label = "MapNavi";
            var handle = Addressables.LoadAssetsAsync<TextAsset>(label, callback: (textAsset) =>
            {
                if (null != textAsset)
                {
                    MapGridData grid = SerializeUtil.Deserialize<MapGridData>(textAsset.bytes);
                    Initialize(grid);
                    Debug.Log($"[TEST][Load Baked Map] {textAsset.name}");
                }
            });
            await handle.Task;
            Addressables.Release(handle);
        }
    }
}