#if UNITY_EDITOR
namespace Script.Data
{
    using Script.Manager;
    using System.Collections.Generic;

    public class EditMapGridData
    {
        public int gridKey;
        public ConcurrentDictionary<int, EditMapTileData> Data;
        public List<string> assetFiles;
        public int[] mesh_asset_instanceIDs;
        public UnityEngine.GameObject gameObject;

        public bool TryGetTileData(int tileIntKey, out EditMapTileData tileData)
        {
            return Data.TryGetValue(tileIntKey, out tileData);
        }

        public void SetChildObjectMeshIDs(int[] ids)
        {
            mesh_asset_instanceIDs = ids;
        }
        public void Dispose()
        {
            for (int i = 0; i < mesh_asset_instanceIDs.Length; ++i)
            {
                AssetManager.Dispose(mesh_asset_instanceIDs[i]);
            }
        }

        public ConcurrentDictionary<int, MapTileData> ParseData()
        {
            ConcurrentDictionary<int, MapTileData> data = new ConcurrentDictionary<int, MapTileData>();

            foreach (var kvp in Data)
            {
                data.TryAdd(kvp.Key, new MapTileData(kvp.Value));
            }

            return data;
        }

        // 이거 괜찮나?
        ~EditMapGridData()
        {
            Dispose();
        }

        public EditMapGridData(int targetGridKey)
        {
            gridKey = targetGridKey;
            Data = new ConcurrentDictionary<int, EditMapTileData>();
            assetFiles = new List<string>();
        }
        public void AddAssetFile(string fileName)
        {
            assetFiles.Add(fileName);
        }
        public bool TryAdd(int key, EditMapTileData navData)
        {
            return Data.TryAdd(key, navData);
        }
    }
}
#endif