namespace Kompile.Data
{
    using UnityEngine;

    public class MapChunk
    {
        public int Layer;
        public GameObject Obj;
        public MeshRenderer Renderer;

        public Color StartColor;
        public Color TargetColor;
        public Color CurrentColor = Color.white;
    }
}