
namespace Script.Map.Data
{
    using Unity.Mathematics;
    using UnityEngine;

    public static class MapConsts
    {
        // triangle
        public const int TRIANGLES_COUNT = 16;
        /// <summary> 타일 내부에서 삼각형을 만들 때에 연결하는 vertex 배열
        /// </summary>
        public static readonly int[] TriangleVertex = new int[]
        {
            0,  3,  1, // virtual_vertex 0,3,1을 연결하면 1번 삼각형이다.
            1,  3,  6,
            3,  5,  6,
            0,  5,  3,
            1,  4,  2,
            2,  4,  7,
            4,  6,  7,
            1,  6,  4,
            5,  8,  6,
            6,  8, 11,
            8, 10, 11,
            5, 10,  8,
            6,  9,  7,
            7,  9, 12,
            9, 11, 12,
            6, 11,  9
        };

        // for on grid
        public static int GRID_SIZE = 64;
        public static int SIZE_TILE = 8;

        public const int TILE_BITS = 6;
        public const int TILE_MASK = (1 << TILE_BITS) - 1;

        public static readonly Vector3[] RELATIVE_COORD_BY_QUARANT = new Vector3[]
        {
            new Vector3(-1f, 0f, -1f), new Vector3( 0f, 0f, -1f), new Vector3( 1f, 0f, -1f),
            new Vector3( 1f, 0f,  0f), new Vector3( 1f, 0f,  1f), new Vector3( 0f, 0f,  1f),
            new Vector3(-1f, 0f,  1f), new Vector3(-1f, 0f,  0f),
        };

        // _SIGN은 '부호(+/-) 플래그'
        public const int SHIFT_GRID_Z = 0;
        public const int SHIFT_GRID_Z_SIGN = 6;
        public const int SHIFT_GRID_Y = 7;
        public const int SHIFT_GRID_Y_SIGN = 13;
        public const int SHIFT_GRID_X = 14;
        public const int SHIFT_GRID_X_SIGN = 20;
        public const int SHIFT_SCENE_INDEX = 21;

        public const int GRID_COORD_SIGNED_MASK = 0b_0111_1111;
        public const int GRID_SIGN_FLAG         = 0b_0100_0000;
        public const int GRID_COORD_MASK        = 0b_0011_1111;

        // 애초에 이게 좀 잘못이었네 ㅇㅋㅇㅋ
        public const int SHIFT_TILE_Z     =  0;
        public const int SHIFT_TILE_Y     =  8;
        public const int SHIFT_TILE_X     = 16;
        public const int SHIFT_TILE_SMALL = 24;
        public const int TILE_COORD_MASK  = 0b_1111_1111;
        //private const int SHIFT_TILE_LAYER  = 25;

        public const int HEIGHT_MASK = 0b_1111;
        public const int HEIGHT_BITS = 4;

        public const long NULL_TILE_NAVI = 0xFFFF_FFFF;
        public const int NULL_TILE_LINK = 0xFFFF;

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

        public const int SPRITE_WIDTH = 256;
        public const int SPRITE_HEIGHT = 256;

        public const int TOTAL_BITS = 13;
        public const int BITS_PER_CELL = 4;
        public const int MATRIX_SIZE = 5;

        // (주의) map_tile_pivot != matrix.Origin(원점)
        public static readonly Vector2Int[] INDEX_MAP = new Vector2Int[]
        {
        new Vector2Int(0, 4), new Vector2Int(2, 4), new Vector2Int(4, 4),
        new Vector2Int(1, 3), new Vector2Int(3, 3),
        new Vector2Int(0, 2), new Vector2Int(2, 2), new Vector2Int(4, 2),
        new Vector2Int(1, 1), new Vector2Int(3, 1),
        new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(4, 0)
        };
    }
}

