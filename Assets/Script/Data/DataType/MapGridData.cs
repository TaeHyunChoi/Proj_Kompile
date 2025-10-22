namespace Script.Data
{
    using MessagePack;
    using Script.Manager;
    using System.Collections.Generic;

    [MessagePackObject]
    public class MapGridData
    {
        [Key(0)]
        [Unity.Collections.ReadOnly] public int gridKey;

        [Key(1)]
        [Unity.Collections.ReadOnly] public ConcurrentDictionary<int, MapTileData> MapNavDataDictionary;

        [Key(2)]
        [Unity.Collections.ReadOnly] public List<GridLayerData> layer_table;

        [IgnoreMember] // 런타임 중에만 사용한다.
        public int[] mesh_asset_instanceIDs;

        [IgnoreMember] // 런타임 중에만 사용한다.
        public UnityEngine.GameObject gameObject;

        [IgnoreMember]
        public List<(int layer, UnityEngine.GameObject obj)> render_objects;





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

        ~MapGridData()
        {
            Dispose();
        }
    }
}
