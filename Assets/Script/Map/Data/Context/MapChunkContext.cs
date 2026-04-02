namespace Script.Map.Data
{
    using UnityEngine;

    public class MapChunkContext
    {
        public int Layer;
        public GameObject Obj;
        public MeshRenderer Renderer;

        public Color StartColor;
        public Color TargetColor;
        public Color CurrentColor = Color.white;
    }
}