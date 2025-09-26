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
    private static readonly Dictionary<DirFlag, float3> DIRECTION = new Dictionary<DirFlag, float3>()
    {
        { DirFlag.LEFT,  new float3(-1f, 0f, 0f) },
        { DirFlag.RIGHT, new float3( 1f, 0f, 0f) },
        { DirFlag.UP,    new float3( 0f, 0f, 1f) },
        { DirFlag.DOWN,  new float3( 0f, 0f,-1f) },
    };

    private static (DirFlag, DirFlag) GetDirectionFlag(float x, float z)
    {
        DirFlag flag_x = DirFlag.NONE;
        DirFlag flag_z = DirFlag.NONE;

        if      (x > 0) { flag_x = DirFlag.RIGHT; }
        else if (x < 0) { flag_x = DirFlag.LEFT; }

        if      (z > 0) { flag_z = DirFlag.UP; }
        else if (z < 0) { flag_z = DirFlag.DOWN; }

        return (flag_x, flag_z);
    }

    private static bool IsLinked(DirFlag direction_flag, EditMapTileData my_tile, EditMapTileData neighbor_tile)
    {
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
        bool compare = false;
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

        return compare;
    }
    private static bool IsChainLinked(ConcurrentDictionary<int, EditMapGridData> map,
                                      EditMapTileData start_tile, EditMapTileData target_tile, 
                                      DirFlag dir_first, DirFlag dir_second)
    {
        float3 mid_pivot = start_tile.GetTilePivot() + DIRECTION[dir_first];
        if (false == EditMapUtil.TryGetTileData(map, mid_pivot, out EditMapTileData mid_tile))
        {
            return false;
        }
        if (false == IsLinked(dir_first, start_tile, mid_tile))
        {
            return false;
        }

        //mid_pivot += DIRECTION[dir_second];
        if (false == EditMapUtil.TryGetTileData(map, mid_pivot, out mid_tile))
        {
            return false;
        }
        if (false == IsLinked(dir_second, mid_tile, target_tile))
        {
            return false;
        }

        return true;
    }
    public static bool TryGetLinkMask(this EditMapTileData my_tile,
                                      ConcurrentDictionary<int, EditMapGridData> map,
                                      EditMapTileData neighbor_tile,
                                      float3 dir,
                                      out int my_link_mask,
                                      out int neighbor_link_mask)
    {
        bool isLinked;
        // 이걸 따로 받는게 나을 듯
        (DirFlag dir_x, DirFlag dir_z) = GetDirectionFlag(dir.x, dir.z);

        my_link_mask        = LINK_NULL;
        neighbor_link_mask  = LINK_NULL;

        DirFlag dir_mask = dir_x | dir_z;
        switch (dir_mask)
        {
            case DirFlag.LEFT:  // (-1, 0)
            case DirFlag.DOWN:  // ( 0,-1)
            case DirFlag.RIGHT: // ( 0, 1)
            case DirFlag.UP:    // ( 1, 0)
                isLinked = IsLinked(dir_x | dir_z, my_tile, neighbor_tile);
                break;

            case DirFlag.LEFT | DirFlag.DOWN:  // (-1,-1)
            case DirFlag.LEFT | DirFlag.RIGHT: // (-1, 1)
            case DirFlag.RIGHT | DirFlag.UP:   // ( 1, 1)
            case DirFlag.RIGHT | DirFlag.DOWN: // ( 1,-1)
                isLinked = IsChainLinked(map, my_tile, neighbor_tile, dir_x, dir_z)
                          || IsChainLinked(map, my_tile, neighbor_tile, dir_z, dir_x);
                break;

            default:
                return false;
        }

        if (true == isLinked)
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
            int my_shift = GetLinkMaskShift(dir_mask);
            int neighbor_shift;
            switch (dir_mask)
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
                    neighbor_shift = GetLinkMaskShift(DirFlag.RIGHT | DirFlag.UP);
                    break;
                case DirFlag.RIGHT | DirFlag.DOWN:
                    neighbor_shift = GetLinkMaskShift(DirFlag.LEFT | DirFlag.UP);
                    break;
                case DirFlag.RIGHT | DirFlag.UP:
                    neighbor_shift = GetLinkMaskShift(DirFlag.LEFT | DirFlag.DOWN);
                    break;
                case DirFlag.LEFT | DirFlag.UP:
                    neighbor_shift = GetLinkMaskShift(DirFlag.RIGHT | DirFlag.DOWN);
                    break;
                default:
                    return false;
            }

            my_link_mask        <<= my_shift;
            neighbor_link_mask  <<= neighbor_shift;
        }

        return isLinked;
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