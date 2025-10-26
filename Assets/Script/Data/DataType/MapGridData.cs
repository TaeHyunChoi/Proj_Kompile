namespace Script.Data
{
    using MessagePack;
    using Unity.Collections;
    using System.Collections.Generic;

    [MessagePackObject]
    public class MapGridData
    {
        [Key(0), ReadOnly] 
        public int gridKey;

        [Key(1), ReadOnly] 
        public ConcurrentDictionary<int, MapTileData> MapNavDataDictionary;

        [Key(2), ReadOnly] 
        public List<GridLayerData> layer_table;

        public bool TryGetTileData(int tileIntKey, out MapTileData tileData)
        {
            return MapNavDataDictionary.TryGetValue(tileIntKey, out tileData);
        }
    }
}
