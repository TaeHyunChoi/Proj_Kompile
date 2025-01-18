namespace Script.Data
{
    using MessagePack;
    using Script.Util;

    [MessagePackObject]
    public class MapGridData
    {
        [Key(0)]
        public ConcurrentDictionary<int, MapNavData> navMeshData;

        public MapGridData()
        {
            navMeshData = new ConcurrentDictionary<int, MapNavData>();
        }

        public bool TryAddNavMeshData(int key, MapNavData navData)
        {
            return navMeshData.TryAdd(key, navData);
        }
    }
}
