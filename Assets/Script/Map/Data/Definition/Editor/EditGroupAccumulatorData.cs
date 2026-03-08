#if UNITY_EDITOR
namespace Script.Map.Provider
{
    using System.Collections.Generic;

    public partial class EditMapSamplingProvider
    {
        private class EditGroupAccumulatorData
        {
            public Queue<EditMapTileChunkData> Tiles;
            public int VertexSum = 0;
            public int PartIndex = 0;

            public EditGroupAccumulatorData()
            {
                Tiles = new Queue<EditMapTileChunkData>();
                VertexSum = 0;
                PartIndex = 0;
            }

            public void Clear()
            {
                Tiles.Clear();
                VertexSum = 0;
                PartIndex = 0;
            }
        }
    }
}
#endif