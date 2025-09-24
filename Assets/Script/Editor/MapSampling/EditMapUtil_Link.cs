#if UNITY_EDITOR

using Script.Data;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

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

    private static readonly int LINK_ZERO = 0b_01;
    private static readonly int LINK_UP   = 0b_10;
    private static readonly int LINK_DOWN = 0b_11;
    private static readonly int LINK_NULL = 0b_00;

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

    private static DirFlag GetDirectionFlag(float x, float z)
    {
        DirFlag flagMask = DirFlag.NONE;

        if      (x > 0) { flagMask |= DirFlag.RIGHT; }
        else if (x < 0) { flagMask |= DirFlag.LEFT; }

        if      (z > 0) { flagMask |= DirFlag.UP; }
        else if (z < 0) { flagMask |= DirFlag.DOWN; }

        return flagMask;
    }

    public static bool TryGetLinkMask(this EditMapTileData my_tile, EditMapTileData neighbor_tile, float3 dir, out int my_link_mask, out int neighbor_link_mask)
    {
        DirFlag direction_flag = GetDirectionFlag(dir.x, dir.z);
        bool compare = false;

        my_link_mask = LINK_NULL;
        neighbor_link_mask = LINK_NULL;

        switch (direction_flag)
        {
            case DirFlag.LEFT:  // (-1, 0)
            case DirFlag.DOWN:  // ( 0,-1)
            case DirFlag.RIGHT: // ( 0, 1)
            case DirFlag.UP:    // ( 1, 0)

                VertexIndexInfo my_vertex_info = my_vertex[direction_flag];
                VertexIndexInfo neighbor_vertex_info = neighbor_vertex[direction_flag];

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

        if (true == compare)
        {
            switch (Mathf.RoundToInt(dir.y))
            {
                case 0:
                    my_link_mask = LINK_ZERO;
                    neighbor_link_mask = LINK_ZERO;
                    break;
                case 1:
                    my_link_mask = LINK_UP;
                    neighbor_link_mask = LINK_DOWN;
                    break;
                case -1:
                    my_link_mask = LINK_DOWN;
                    neighbor_link_mask = LINK_UP;
                    break;
                default:
                    return false;
            }

            // my, neighbor shift가 다르구나..
            // 일단 구현은 다 하고 코드 수정하는 것으로...
            int my_shift = GetLinkMaskShift(direction_flag);
            int neighbor_shift;
            switch (direction_flag)
            {
                case DirFlag.DOWN:
                    neighbor_shift = GetLinkMaskShift(DirFlag.UP);
                    break;
                case DirFlag.RIGHT:
                    neighbor_shift = GetLinkMaskShift(DirFlag.LEFT);
                    break;
                case DirFlag.UP:
                    neighbor_shift = GetLinkMaskShift(DirFlag.DOWN);
                    break;
                case DirFlag.LEFT:
                    neighbor_shift = GetLinkMaskShift(DirFlag.RIGHT);
                    break;

                case DirFlag.LEFT | DirFlag.DOWN:
                case DirFlag.RIGHT | DirFlag.DOWN:
                case DirFlag.RIGHT | DirFlag.UP:
                case DirFlag.LEFT | DirFlag.UP:
                    // 잠시 대기...
                default:
                    return false;
            }

            my_link_mask        <<= my_shift;
            neighbor_link_mask  <<= neighbor_shift;
        }

        return compare;
    }

    private static int GetLinkMaskShift(DirFlag direction_flag)
    {
        int shift = 0;
        switch (direction_flag)
        {
            case DirFlag.LEFT | DirFlag.DOWN:
                shift = 0 * 2;
                break;
            case DirFlag.DOWN:
                shift = 1 * 2;
                break;
            case DirFlag.RIGHT | DirFlag.DOWN:
                shift = 2 * 2;
                break;
            case DirFlag.RIGHT:
                shift = 2 * 3;
                break;
            case DirFlag.RIGHT | DirFlag.UP:
                shift = 2 * 4;
                break;
            case DirFlag.UP:
                shift = 2 * 5;
                break;
            case DirFlag.LEFT | DirFlag.UP:
                shift = 2 * 6;
                break;
            case DirFlag.LEFT:
                shift = 2 * 7;
                break;
        }

        return shift;
    }
}
#endif