namespace Script.Data
{
    using MessagePack;
    using Script.Manager;
    using Script.Util;
    using System.Collections.Generic;

    [MessagePackObject]
    public class MapGridData
    {
        [Key(0)]
        public int gridKey;

        [Key(1)]
        public ConcurrentDictionary<int, MapTileData> MapNavDataDictionary;

        [Key(2)]
        public List<string> assetFiles;

        [IgnoreMember] // 런타임 중에만 사용한다.
        public int[] mesh_asset_instanceIDs;

        [IgnoreMember] // 런타임 중에만 사용한다.
        public UnityEngine.GameObject gameObject;

        public int GetSceneIndex() => gridKey >> 24;

        public bool ContainTile(int tKey)
        {
            return MapNavDataDictionary.ContainsKey(tKey);
        }
        public bool TryGetTileData(int tileIntKey, out MapTileData tileData)
        {
            return MapNavDataDictionary.TryGetValue(tileIntKey, out tileData);
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

        // 이거 괜찮나?
        ~MapGridData()
        {
            Dispose();
        }

#if UNITY_EDITOR
        public MapGridData(int targetGridKey)
        {
            gridKey = targetGridKey;
            MapNavDataDictionary = new ConcurrentDictionary<int, MapTileData>();
            assetFiles = new List<string>();
        }
        public void AddAssetFile(string fileName)
        {
            assetFiles.Add(fileName);
        }
        public bool TryAdd(int key, MapTileData navData)
        {
            return MapNavDataDictionary.TryAdd(key, navData);
        }
#endif
    }


#if UNITY_EDITOR
    [MessagePackObject]
    public class EditMapGridData
    {
        [Key(0)]
        public int gridKey;
        [Key(1)]
        public ConcurrentDictionary<int, MapTileData> Data;
        [Key(2)]
        public List<string> assetFiles;
        [IgnoreMember]
        public int[] mesh_asset_instanceIDs;
        [IgnoreMember]
        public UnityEngine.GameObject gameObject;

        public bool TryGetTileData(int tileIntKey, out MapTileData tileData)
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

        // 이거 괜찮나?
        ~EditMapGridData()
        {
            Dispose();
        }

        public EditMapGridData(int targetGridKey)
        {
            gridKey = targetGridKey;
            Data = new ConcurrentDictionary<int, MapTileData>();
            assetFiles = new List<string>();
        }
        public void AddAssetFile(string fileName)
        {
            assetFiles.Add(fileName);
        }
        public bool TryAdd(int key, MapTileData navData)
        {
            return Data.TryAdd(key, navData);
        }
    }
#endif
}
