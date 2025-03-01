namespace Script.Manager
{
    using Script.Data;
    using Script.Util;

    public class FieldMapManager
    {
        private ConcurrentDictionary<int, MapGridData> mapGridData;

        public FieldMapManager()
        {
            mapGridData = new ConcurrentDictionary<int, MapGridData>();
        }

        public bool TryAddMapGridData(int gridKey, MapGridData targetData)
        {
            return mapGridData.TryAdd(gridKey, targetData);
        }
    }
}
