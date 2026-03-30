#if UNITY_EDITOR
namespace Script.Map.Provider
{
    using System.Collections.Generic;
    
    public partial class EditMapSamplingRepoProvider // EditBakeAccumulator
    {
        private class EditBakeAccumulator
        {
            public int VertexSum;
            public int PartIndex;
            public Queue<EditBakeChunkData> Tiles = new Queue<EditBakeChunkData>();

            public void Clear()
            {
                VertexSum = 0; 
                Tiles.Clear();
            }
        }
    }
}
#endif