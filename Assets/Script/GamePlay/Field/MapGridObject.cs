namespace Script.Map
{
    using Script.Asset;
    using Script.Data;
    using System.Collections.Generic;
    using System.Net;
    using UnityEngine;

    public class MapGridObject : IngameMonoBehaviourBase
    {
        public override PrefabID PrefabID => PrefabID.MapGridPrefab;

        private List<(int, GameObject)> layerObjects;

        public void Initialize(List<(int, Mesh)> values)
        {
            int count = values.Count;
            layerObjects = new List<(int, GameObject)>(values.Count);

            GameObject obj;
            MeshFilter meshFilter;
            MeshRenderer meshRender;
            for (int i = 0; i < count; ++i)
            {
                obj = new GameObject();
                obj.transform.SetParent(this.transform);

                meshFilter = obj.AddComponent<MeshFilter>();
                meshFilter.mesh = values[i].Item2;
                layerObjects.Add((values[i].Item1, obj));

                meshRender = obj.AddComponent<MeshRenderer>();
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < layerObjects.Count; ++i)
            {
                int id = layerObjects[i].Item2.GetInstanceID();
                AssetSystem.ReleaseAsset(id);
            }
        }
    }
}