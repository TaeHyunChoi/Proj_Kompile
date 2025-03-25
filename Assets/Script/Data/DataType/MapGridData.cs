namespace Script.Data
{
    using MessagePack;

    [MessagePackObject]
    public class MapGridData
    {
        [Key(0)]
        public int gridKey;

        [Key(1)]
        public ConcurrentDictionary<int, MapNavData> MapNavDataDictionary;

        // trigger 정보는 MapNavData.infoMask로 빠질라나 => 이거 개념이 뭐임?
        // MapGridData에서 무엇을 들고 있어야 할까?

        public MapGridData(int targetGridKey)
        {
            gridKey = targetGridKey;
            MapNavDataDictionary = new ConcurrentDictionary<int, MapNavData>();
        }

        public bool TryAddNavMeshData(int key, MapNavData navData)
        {
            return MapNavDataDictionary.TryAdd(key, navData);
        }
    }
}
