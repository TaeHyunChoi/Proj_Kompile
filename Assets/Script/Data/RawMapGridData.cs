namespace Script.Data
{
    using MessagePack;
    using Script.Util;

    [MessagePackObject]
    public class RawMapGridData
    {
        [Key(0)]
        public ConcurrentDictionary<int, RawMapNavData> rawMapNavData;

        public RawMapGridData()
        {
            rawMapNavData = new ConcurrentDictionary<int, RawMapNavData>();
        }

        public bool TryAddNavMeshData(int key, RawMapNavData navData)
        {
            return rawMapNavData.TryAdd(key, navData);
        }
    }
}
