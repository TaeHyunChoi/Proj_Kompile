namespace Script.Map.Provider
{
    using System;
    
    public partial class EditMapSamplingRepoProvider
    {
        private readonly struct EditMapGroupKey : IEquatable<EditMapGroupKey>
        {
            public readonly int RenderLayer;
            public readonly int GridKey;
            public EditMapGroupKey(int layer, int gKey)
            {
                RenderLayer = layer;
                GridKey = gKey;
            }

            public bool Equals(EditMapGroupKey other)
            {
                return RenderLayer == other.RenderLayer && GridKey == other.GridKey;
            }

            public override bool Equals(object obj)
            {
                return obj is EditMapGroupKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = (hash * 397) ^ RenderLayer.GetHashCode();
                    hash = (hash * 397) ^ GridKey.GetHashCode();
                    return hash;
                }
            }
        }   
    }
}