#if UNITY_EDITOR
namespace Kompile.Editor.Domain
{
    using System.Collections.Generic;
    using Kompile.Editor.Data;

    public partial class EditMapSamplingProvider // EditBakeAccumulator
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