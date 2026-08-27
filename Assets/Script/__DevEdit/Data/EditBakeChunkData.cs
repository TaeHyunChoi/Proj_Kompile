#if UNITY_EDITOR
namespace Kompile.Editor.Data
{
    using UnityEngine;

    public class EditBakeChunkData
    {
        public CombineInstance Instance;
        public int VertexCount;
        public ushort RenderLayer;
        public int GridKey;
        public int TopTextureIndex;
        public int SideTextureIndex;
    }
}
#endif