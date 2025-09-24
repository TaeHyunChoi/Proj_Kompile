#if UNITY_EDITOR

using Script.Data;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Script.Index.MapTileIndex;

public static partial class EditMapUtil
{
    [Flags]
    private enum DirFlag
    { 
        NONE    = 0,
        UP      = 1,
        DOWN    = 1 << 1,
        LEFT    = 1 << 2,
        RIGHT   = 1 << 3
    }
    private readonly struct VertexIndexInfo
    {
        public readonly int center;
        public readonly int side_0;
        public readonly int side_1;

        public VertexIndexInfo(int c, int s0, int s1)
        {
            center = c;
            side_0 = s0;
            side_1 = s1;
        }
    }
    private static readonly Dictionary<DirFlag, VertexIndexInfo> my_vertex = new Dictionary<DirFlag, VertexIndexInfo>()
    {
        { DirFlag.LEFT,  new VertexIndexInfo(5, 0, 10) },
        { DirFlag.DOWN,  new VertexIndexInfo(1, 0, 2) },
        { DirFlag.UP,    new VertexIndexInfo(11, 10, 12) },
        { DirFlag.RIGHT, new VertexIndexInfo(7, 2, 12)}
    };
    private static readonly Dictionary<DirFlag, VertexIndexInfo> neighbor_vertex = new Dictionary<DirFlag, VertexIndexInfo>()
    {
        { DirFlag.LEFT,  new VertexIndexInfo(7, 2, 12) },
        { DirFlag.DOWN,  new VertexIndexInfo(11, 10, 12) },
        { DirFlag.UP,    new VertexIndexInfo(1, 0, 2)},
        { DirFlag.RIGHT, new VertexIndexInfo(5, 0, 10)}
    };

    public static bool IsLinkedTo(this EditMapTileData my_tile, EditMapTileData neighbor_tile, float2 dir)
    {
        // 경우를 일반화 하려면?
        DirFlag flagMask = DirFlag.NONE;
        bool compare = false;

        if      (dir.x > 0) { flagMask |= DirFlag.RIGHT; }
        else if (dir.x < 0) { flagMask |= DirFlag.LEFT;  }

        if      (dir.y > 0) { flagMask |= DirFlag.UP;    }
        else if (dir.y < 0) { flagMask |= DirFlag.DOWN;  }

        switch (flagMask)
        {
            case DirFlag.LEFT:
            case DirFlag.DOWN: // ( 0,-1)
            case DirFlag.RIGHT: // ( 0, 1)
            case DirFlag.UP: // ( 1, 0)

                VertexIndexInfo my_vertex_info = my_vertex[flagMask];
                VertexIndexInfo neighbor_vertex_info = neighbor_vertex[flagMask];

                             // 중앙점 비교
                if (false == my_tile.TryGetVerticeHeight(my_vertex_info.center, out int my_height_1000x)
                    || false == neighbor_tile.TryGetVerticeHeight(neighbor_vertex_info.center, out int neighbor_height_1000x))
                {
                    return false;
                }
                if (my_height_1000x != neighbor_height_1000x)
                {
                    return false;
                }

                // 양옆의 점 높이 비교

                if (true == my_tile.TryGetVerticeHeight(my_vertex_info.side_0, out my_height_1000x)
                    && true == neighbor_tile.TryGetVerticeHeight(neighbor_vertex_info.side_0, out neighbor_height_1000x))
                {
                    compare |= my_height_1000x == neighbor_height_1000x;
                }
                if (true == my_tile.TryGetVerticeHeight(my_vertex_info.side_1, out my_height_1000x)
                    && true == neighbor_tile.TryGetVerticeHeight(neighbor_vertex_info.side_1, out neighbor_height_1000x))
                {
                    compare |= my_height_1000x == neighbor_height_1000x;
                }


                break;

            case DirFlag.LEFT | DirFlag.DOWN:  // (-1,-1)
            case DirFlag.LEFT | DirFlag.RIGHT: // (-1, 1)
            case DirFlag.RIGHT | DirFlag.UP:   // ( 1, 1)
            case DirFlag.RIGHT | DirFlag.DOWN: // ( 1,-1)
                // 추후에 다른 로직을 사용
                return false;

            default:
                return false;
        }

        return compare;
    }
}
#endif