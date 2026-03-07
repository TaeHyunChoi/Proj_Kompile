#if UNITY_EDITOR

using Script.Data;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

/// <summary> 맵타일 관련하여 '에디터'에서만 사용하는 함수.
/// 비슷한 기능으로 함수가 많아지니까 헷갈려서 분리;
/// 개발하면서 함수 기능은 다시 정리하던가 그럽시다.
/// </summary>
public static partial class EditMapUtil
{
    public const int SPRITE_WIDTH   = 256;
    public const int SPRITE_HEIGHT  = 256;

    public const int TOTAL_BITS     = 13;
    public const int BITS_PER_CELL  = 4;
    public const int MATRIX_SIZE    = 5;

    public const int GRID_SIZE = 64;
    public const int SIZE_TILE = 8;
    public const int TILE_BITS = 6;
    public const int TILE_MASK = (1 << TILE_BITS) - 1;

    public static EditMapTileDirFlag GetDirFlag(float x, float z)
    {
        EditMapTileDirFlag flag = EditMapTileDirFlag.NONE;

        if (x > 0) { flag |= EditMapTileDirFlag.RIGHT; }
        else if (x < 0) { flag |= EditMapTileDirFlag.LEFT; }

        if (z > 0) { flag |= EditMapTileDirFlag.UP; }
        else if (z < 0) { flag |= EditMapTileDirFlag.DOWN; }

        return flag;
    }
    public static EditVertexIndexInfo GetVertexIndexInfo(EditMapTileDirFlag flag)
    {
        return flag switch
        {
            EditMapTileDirFlag.LEFT => new EditVertexIndexInfo(5, 0, 10),
            EditMapTileDirFlag.RIGHT => new EditVertexIndexInfo(7, 2, 12),
            EditMapTileDirFlag.UP => new EditVertexIndexInfo(11, 10, 12),
            EditMapTileDirFlag.DOWN => new EditVertexIndexInfo(1, 0, 2),
            _ => default
        };
    }
    public static float3 GetDirectionVector(EditMapTileDirFlag flag)
    {
        return flag switch
        {
            EditMapTileDirFlag.LEFT => new float3(-1f, 0f, 0f),
            EditMapTileDirFlag.RIGHT => new float3(1f, 0f, 0f),
            EditMapTileDirFlag.UP => new float3(0f, 0f, 1f),
            EditMapTileDirFlag.DOWN => new float3(0f, 0f, -1f),
            _ => default
        };
    }
}
#endif