#if UNITY_EDITOR
namespace Kompile.Editor.Provider
{
    using UnityEngine;

    public partial class EditMapSamplingProvider //EditBakeChunkData
    {
        private class EditBakeChunkData
        {
            public CombineInstance Instance;
            public int VertexCount;
            public ushort RenderLayer;
            public int GridKey;
            public int TopTextureIndex;
            public int SideTextureIndex;
        }
    }
}
#endif