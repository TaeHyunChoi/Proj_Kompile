namespace Script.Map.Data
{
    using MessagePack;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    
    [MessagePackObject]
    public class MapGridData
    {
        [Key(0)] public int Key;
        [Key(1)] public ConcurrentDictionary<int, MapTileData> NaviTileDict;
        [Key(2)] public List<MapGridLayerData> layerMeshAssets;

        // MessagePack 역직렬화용 생성자 명시
        [SerializationConstructor]
        public MapGridData() { }

        public bool TryGetTileData(int tileIntKey, out MapTileData tileData)
        {
            return NaviTileDict.TryGetValue(tileIntKey, out tileData);
        }
    }
}