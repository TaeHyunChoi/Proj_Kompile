namespace Script.Data
{
    using MessagePack;
    using Script.Manager;
    using System.Collections.Generic;

    [MessagePackObject]
    public class MapGridData
    {
        [Key(0)]
        public int gridKey;

        [Key(1)]
        public ConcurrentDictionary<int, MapNavData> MapNavDataDictionary;

        [Key(2)]
        public List<string> assetFiles;

        [IgnoreMember] // 런타임 중에만 사용한다.
        public int[] mesh_asset_instanceIDs;

        public void SetChildObjectMeshIDs(int[] ids)
        {
            mesh_asset_instanceIDs = ids;
        }
        //~MapGridData()
        //{
        //    // 이게 여기에 있으면 안되는거구나?
        //    AssetManager.Dispose(mesh_asset_instanceIDs);
        //    mesh_asset_instanceIDs = null;
        //}

#if UNITY_EDITOR
        public MapGridData(int targetGridKey)
        {
            gridKey = targetGridKey;
            MapNavDataDictionary = new ConcurrentDictionary<int, MapNavData>();
            assetFiles = new List<string>();
        }
        public void AddAssetFile(string fileName)
        {
            assetFiles.Add(fileName);
        }
        public bool TryAddNavMeshData(int key, MapNavData navData)
        {
            return MapNavDataDictionary.TryAdd(key, navData);
        }
#endif
    }
}
