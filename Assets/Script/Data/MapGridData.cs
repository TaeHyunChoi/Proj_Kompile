namespace Script.Data
{
    using MessagePack;
    using Script.Util;

    [MessagePackObject]
    public class MapGridData
    {
        [Key(0)]
        public int gridKey;

        [Key(1)]
        public ConcurrentDictionary<int, RawMapNavData> rawMapNavData;

        // trigger 정보는 RawMapNaviData.infoMask로 빠질라나 => 이거 개념이 뭐임?
        // MapGridData에서 무엇을 들고 있어야 할까?

        public MapGridData(int targetGridKey)
        {
            gridKey = targetGridKey;
            rawMapNavData = new ConcurrentDictionary<int, RawMapNavData>();
        }

        public bool TryAddNavMeshData(int key, RawMapNavData navData)
        {
            return rawMapNavData.TryAdd(key, navData);
        }
    }
}
