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

            gridLayerDict = new Dictionary<int, List<GridLayerData>>();
            tileDict = new Dictionary<long, MapTileData>();

            posToID = new Dictionary<int3, long>(capacity: count);

            int gKey = grid.Key;

            // layer info
            gridLayerDict.Add(gKey, grid.layerMeshAssets);

            // tile navi info
            tileDict = new Dictionary<long, MapTileData>();
            foreach (var tileKV in grid.NaviTileDict)
            {
                int tKey = tileKV.Key;
                MapTileData tile = tileKV.Value;

                long id = MapPathUtil.ComputeID(gKey, tKey);
                if (false == tileDict.TryAdd(id, tile))
                {
                    tileDict[id] = tile;
                }

                int3 absPivot = MapPathUtil.ComputeAbsoluteWorldPosition(id);
                posToID.TryAdd(absPivot, id);
            }
        }


        public async Awaitable EditLoadAll()
        {
            // Addressable Label로 모든 MapNavi.bytes 불러오기
            string label = "MapNavi";
            var locations = await Addressables.LoadResourceLocationsAsync(label).Task;
            if (null == locations || 0 == locations.Count)
            {
                Debug.Log($"'{label}'로 지정된 파일을 찾을 수 없습니다.");
                return;
            }

            foreach (var location in locations)
            {
                await LoadFromAddressableAsync(location.PrimaryKey);
                foreach (KeyValuePair<long, MapTileData> tileKV in TileDic)
                {
                    var id = tileKV.Key;
                    var tile = tileKV.Value;
                    var pivot = MapPathUtil.ComputeWorldPosition(id);

                    Debug.Log($"[{id}] {pivot}.navi = {System.Convert.ToString(tile.NaviMask, 16)},\nlink = {System.Convert.ToString(tile.LinkMask, 2)}");
                }
            }
        }
    }
}
