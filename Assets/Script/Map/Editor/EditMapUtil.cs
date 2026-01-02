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

    // (주의) map_tile_pivot != matrix.Origin(원점)
    public static readonly Vector2Int[] INDEX_MAP = new Vector2Int[]
    {
        new Vector2Int(0, 4), new Vector2Int(2, 4), new Vector2Int(4, 4),
        new Vector2Int(1, 3), new Vector2Int(3, 3),
        new Vector2Int(0, 2), new Vector2Int(2, 2), new Vector2Int(4, 2),
        new Vector2Int(1, 1), new Vector2Int(3, 1),
        new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(4, 0)
    };

    public static long ComputeTileID(Vector3 worldPos)
    {
        int absTx = Mathf.FloorToInt(worldPos.x);
        int absTy = Mathf.FloorToInt(worldPos.y);
        int absTz = Mathf.FloorToInt(worldPos.z);

        int gX = Mathf.FloorToInt((float)absTx / GRID_SIZE);
        int gY = Mathf.FloorToInt((float)absTy / GRID_SIZE);
        int gZ = Mathf.FloorToInt((float)absTz / GRID_SIZE);

        int tX = absTx - gX * GRID_SIZE;
        int tY = absTy - gY * GRID_SIZE;
        int tZ = absTz - gZ * GRID_SIZE;

        if (tX < 0) { tX += GRID_SIZE; gX -= 1; }
        if (tY < 0) { tY += GRID_SIZE; gY -= 1; }
        if (tZ < 0) { tZ += GRID_SIZE; gZ -= 1; }

        uint tKey =
            (uint)((tX & TILE_MASK) << (TILE_BITS * 2)) |
            (uint)((tY & TILE_MASK) << (TILE_BITS * 1)) |
            (uint)((tZ & TILE_MASK) << (TILE_BITS * 0));

        byte bX = (byte)(sbyte)gX;
        byte bY = (byte)(sbyte)gY;
        byte bZ = (byte)(sbyte)gZ;

        uint gKey = (uint)((bX << 16) | (bY << 8) | bZ);
        return (((long)gKey) << 32) | tKey;
    }
    public static long ComputeID(int gKey, int tKey)
    {
        return (((long)gKey) << 32) | (uint)tKey;
    }

    public static void ComputeKey(float3 worldPos, out int outGKey, out int outTKey)
    {
        int absTx = Mathf.FloorToInt(worldPos.x);
        int absTy = Mathf.FloorToInt(worldPos.y);
        int absTz = Mathf.FloorToInt(worldPos.z);

        int gX = Mathf.FloorToInt((float)absTx / GRID_SIZE);
        int gY = Mathf.FloorToInt((float)absTy / GRID_SIZE);
        int gZ = Mathf.FloorToInt((float)absTz / GRID_SIZE);

        int tX = absTx - gX * GRID_SIZE;
        int tY = absTy - gY * GRID_SIZE;
        int tZ = absTz - gZ * GRID_SIZE;

        if (tX < 0) { tX += GRID_SIZE; gX -= 1; }
        if (tY < 0) { tY += GRID_SIZE; gY -= 1; }
        if (tZ < 0) { tZ += GRID_SIZE; gZ -= 1; }

        outTKey = ((tX & TILE_MASK) << (TILE_BITS * 2))
                | ((tY & TILE_MASK) << (TILE_BITS * 1))
                | ((tZ & TILE_MASK) << (TILE_BITS * 0));

        byte bX = (byte)(sbyte)gX;
        byte bY = (byte)(sbyte)gY;
        byte bZ = (byte)(sbyte)gZ;

        outGKey = ((bX << 16) | (bY << 8) | bZ);
    }
    public static void ComputeKey(long id, out int outGKey, out int outTKey)
    {
        float3 position = ComputeWorldPosition(id);
        ComputeKey(position, out outGKey, out outTKey);
    }
    public static int ComputeGridKey(float3 worldPos)
    {
        int absTx = Mathf.FloorToInt(worldPos.x);
        int absTy = Mathf.FloorToInt(worldPos.y);
        int absTz = Mathf.FloorToInt(worldPos.z);

        int gX = Mathf.FloorToInt((float)absTx / GRID_SIZE);
        int gY = Mathf.FloorToInt((float)absTy / GRID_SIZE);
        int gZ = Mathf.FloorToInt((float)absTz / GRID_SIZE);

        byte bX = (byte)(sbyte)gX;
        byte bY = (byte)(sbyte)gY;
        byte bZ = (byte)(sbyte)gZ;

        // use only 3 bytes;
        return (bX << 16) | (bY << 8) | (bZ << 0);
    }

    public static float3 ComputeWorldPosition(long id)
    {
        int3 absPos = ComputeAbsoluteWorldPosition(id);
        return new Vector3(absPos.x, absPos.y, absPos.z);
    }
    private static int3 ComputeAbsoluteWorldPosition(long id)
    {
        uint gKey = (uint)((ulong)id >> 32);
        uint tKey = (uint)id;

        int gx = (sbyte)(byte)((gKey >> 16) & 0xFF);
        int gy = (sbyte)(byte)((gKey >> 8) & 0xFF);
        int gz = (sbyte)(byte)((gKey >> 0) & 0xFF);

        int tx = (int)((tKey >> (TILE_BITS * 2)) & TILE_MASK);
        int ty = (int)((tKey >> (TILE_BITS * 1)) & TILE_MASK);
        int tz = (int)((tKey >> (TILE_BITS * 0)) & TILE_MASK);

        return new int3(
            gx * GRID_SIZE + tx,
            gy * GRID_SIZE + ty,
            gz * GRID_SIZE + tz);
    }

    public static bool TryGetLinkTileIndex(float2 dir, out int index)
    {
        EditMapTileDirFlag flag_x = EditMapTileDirFlag.NONE;
        EditMapTileDirFlag flag_z = EditMapTileDirFlag.NONE;

        if (dir.x > 0) { flag_x = EditMapTileDirFlag.RIGHT; }
        else if (dir.x < 0) { flag_x = EditMapTileDirFlag.LEFT; }

        // 사실은 z값
        if (dir.y > 0) { flag_z = EditMapTileDirFlag.UP; }
        else if (dir.y < 0) { flag_z = EditMapTileDirFlag.DOWN; }

        index = -1;
        switch (flag_x | flag_z)
        {
            case EditMapTileDirFlag.DOWN | EditMapTileDirFlag.LEFT: index = 0; break;
            case EditMapTileDirFlag.DOWN: index = 1; break;
            case EditMapTileDirFlag.DOWN | EditMapTileDirFlag.RIGHT: index = 2; break;
            case EditMapTileDirFlag.RIGHT: index = 3; break;
            case EditMapTileDirFlag.UP | EditMapTileDirFlag.RIGHT: index = 4; break;
            case EditMapTileDirFlag.UP: index = 5; break;
            case EditMapTileDirFlag.UP | EditMapTileDirFlag.LEFT: index = 6; break;
            case EditMapTileDirFlag.LEFT: index = 7; break;
            default:
                return false;
        }

        return true;
    }

    public static int GetLinkMaskShift(EditMapTileDirFlag flag)
    {
        // 반시계 방향으로 돌린다~!!
        return 2 * flag switch
        {
            EditMapTileDirFlag.LEFT | EditMapTileDirFlag.DOWN => 0,
            EditMapTileDirFlag.DOWN => 1,
            EditMapTileDirFlag.RIGHT | EditMapTileDirFlag.DOWN => 2,
            EditMapTileDirFlag.RIGHT => 3,
            EditMapTileDirFlag.RIGHT | EditMapTileDirFlag.UP => 4,
            EditMapTileDirFlag.UP => 5,
            EditMapTileDirFlag.LEFT | EditMapTileDirFlag.UP => 6,
            EditMapTileDirFlag.LEFT => 7,
            _ => -1
        };
    }


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