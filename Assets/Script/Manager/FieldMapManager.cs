namespace Script.Manager
{
    using Script.Data;
    using Script.Util;

    public class FieldMapManager
    {
        private ConcurrentDictionary<int, RawMapGridData> rawMapGridData;

        public FieldMapManager()
        {
            rawMapGridData = new ConcurrentDictionary<int, RawMapGridData>();
        }

        public bool TryAddRawMapGridData(int gridKey, RawMapGridData targetData)
        {
            return rawMapGridData.TryAdd(gridKey, targetData);
        }
    }
}
