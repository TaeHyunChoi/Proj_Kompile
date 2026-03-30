namespace Script.Map.Provider
{
    using UnityEngine;

    public partial class EditMapSamplingRepoProvider //EditBakeChunkData
    {
        private class EditBakeChunkData
        {
            public CombineInstance Instance;
            public int    VertexCount;
            public ushort RenderLayer;
            public int    GridKey;
            public int    TopTextureIndex;
            public int    SideTextureIndex;
        }
    }
}