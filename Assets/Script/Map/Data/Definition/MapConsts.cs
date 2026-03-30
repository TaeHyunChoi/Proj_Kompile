
namespace Script.Map.Data
{
    using Unity.Mathematics;

    public static class MapConsts
    {
        public const  int TRIANGLES_COUNT = 16;
        public static int GRID_SIZE       = 64;
        public const  int TILE_BITS       = 6;
        public const  int TILE_MASK       = (1 << TILE_BITS) - 1;
        public const  int HEIGHT_MASK     = 0b_1111;
        public const  int HEIGHT_BITS     = 4;

        /// <summary> 서브 타일을 구성하는 3개의 정점 모음 </summary>
        public static readonly int[] SubTileVertexMap = new int[]
        {
            0, 1, 3,    // s0
            1, 3, 6,    // s1
            3, 5, 6,    // s2
            0, 3, 5,    // s3
            1, 2, 4,    // s4
            2, 4, 7,    // s5
            4, 6, 7,    // s6
            1, 4, 6,    // s7
            5, 6, 8,    // s8
            6, 8, 11,   // s9
            8, 10, 11,  // s10
            5, 8, 10,   // s11
            6, 7, 9,    // s12
            7, 9, 12,   // s13
            9, 11, 12,  // s14
            6, 9, 11    // s15            
        };

        /// <summary> 그림의 v00 ~ v12 위치를 2D 좌표로 매핑 </summary>
        public static readonly float2[] VertexPositions = new float2[]
        {
        new float2(0.00f, 0.00f), // v00
        new float2(0.50f, 0.00f), // v01
        new float2(1.00f, 0.00f), // v02
        new float2(0.25f, 0.25f), // v03 (Center of Bottom-Left Quad)
        new float2(0.75f, 0.25f), // v04 (Center of Bottom-Right Quad)
        new float2(0.00f, 0.50f), // v05
        new float2(0.50f, 0.50f), // v06 (Center of Tile)
        new float2(1.00f, 0.50f), // v07
        new float2(0.25f, 0.75f), // v08 (Center of Top-Left Quad)
        new float2(0.75f, 0.75f), // v09 (Center of Top-Right Quad)
        new float2(0.00f, 1.00f), // v10
        new float2(0.50f, 1.00f), // v11
        new float2(1.00f, 1.00f)  // v12
        };

        public const int TOTAL_BITS = 13;
        public const int BITS_PER_CELL = 4;

        
#if UNITY_EDITOR
        [System.Flags]
        public enum EditMapTileDirFlag
        {
            NONE    = 0,
            UP      = 1 << 0,
            DOWN    = 1 << 1,
            LEFT    = 1 << 2,
            RIGHT   = 1 << 3
        }
#endif
    }
}

