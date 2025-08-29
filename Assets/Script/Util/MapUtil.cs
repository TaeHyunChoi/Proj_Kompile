namespace Script.Util
{
    using static Index.MapTileIndex;
    using UnityEngine;

    // => MapTileInfo, Coordinate
    public static partial class MapUtil 
    {
        public static Vector3 GetVertexPoint(int virtual_index, float y)
        {
            float x = 0f, z = 0f;
            switch (virtual_index)
            {
                case  0: x = 0.00f; z = 0.00f; break;
                case  1: x = 0.50f; z = 0.00f; break;
                case  2: x = 1.00f; z = 0.00f; break;
                case  3: x = 0.25f; z = 0.25f; break;
                case  4: x = 0.75f; z = 0.25f; break;
                case  5: x = 0.00f; z = 0.50f; break;
                case  6: x = 0.50f; z = 0.50f; break;
                case  7: x = 1.00f; z = 0.50f; break;
                case  8: x = 0.25f; z = 0.75f; break;
                case  9: x = 0.75f; z = 0.75f; break;
                case 10: x = 0.00f; z = 1.00f; break;
                case 11: x = 0.50f; z = 1.00f; break;
                case 12: x = 1.00f; z = 1.00f; break;
            }

            return new Vector3(x, y, z);
        }
    }
}
