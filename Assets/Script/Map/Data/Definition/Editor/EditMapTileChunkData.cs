#if UNITY_EDITOR
namespace Script.Map.Provider
{
    using UnityEngine;

    public partial class EditMapSamplingRepoProvider
    {
        private class EditMapTileChunkData
        {
            public CombineInstance Instance;
            public Vector2[] UVs;
            public int VertexCount;
            public int GridKey;
            public int RenderLayer;

            public void Clear()
            {
                Instance.mesh = null;
                Instance = default;
                UVs = null;
                VertexCount = 0;
                GridKey = 0;
                RenderLayer = 0;
            }
        }
    }
}
#endif