#if UNITY_EDITOR

using Script.Data;
using Unity.Mathematics;
using UnityEngine;
using static Script.Index.MapTileIndex;

/// <summary> 맵타일 관련하여 '에디터'에서만 사용하는 함수.
/// 비슷한 기능으로 함수가 많아지니까 헷갈려서 분리;
/// 개발하면서 함수 기능은 다시 정리하던가 그럽시다.
/// </summary>
public static partial class EditMapUtil
{
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

    public static int GetGridKeyMask(int sceneIndex, float3 rotated_position)
    {
        int sceneIndexMask = sceneIndex << SHIFT_SCENE_INDEX;
        int gridPivotMask  = GetGridKeyMask(rotated_position);

        return sceneIndexMask | gridPivotMask;
    }

    public static int GetGridKeyMask(float3 position)
    {
        int mask = 0;
        int gx = Mathf.FloorToInt(position.x / SIZE_GRID_AXIS);
        int gy = Mathf.FloorToInt(position.y / SIZE_GRID_AXIS);
        int gz = Mathf.FloorToInt(position.z / SIZE_GRID_AXIS);

        if (gx < 0)
        {
            mask |= 1 << SHIFT_GRID_X_SIGN;
            gx *= -1;
        }
        mask |= gx << SHIFT_GRID_X;

        if (gy < 0)
        {
            mask |= 1 << SHIFT_GRID_Y_SIGN;
            gy *= -1;
        }
        mask |= gy << SHIFT_GRID_Y;

        if (gz < 0)
        {
            mask |= 1 << SHIFT_GRID_Z_SIGN;
            gz *= -1;
        }
        mask |= gz << SHIFT_GRID_Z;

        return mask;
    }
    public static int GetTileKeyMask(float3 position)
    {
        int x = Mathf.RoundToInt(position.x % SIZE_GRID_AXIS);
        if (x < 0)
        {
            x += SIZE_GRID_AXIS;
        }

        int y = Mathf.RoundToInt(position.y % SIZE_GRID_AXIS);
        if (y < 0)
        {
            y += SIZE_GRID_AXIS;
        }

        int z = Mathf.RoundToInt(position.z % SIZE_GRID_AXIS);
        if (z < 0)
        {
            z += SIZE_GRID_AXIS;
        }

        int tileKeyMask = 0;
        tileKeyMask |= x << SHIFT_TILE_X;
        tileKeyMask |= y << SHIFT_TILE_Y;
        tileKeyMask |= z << SHIFT_TILE_Z;

        return tileKeyMask;
    }

    public static float3 GetGridPosition(int gKey)
    {
        int gx = (gKey >> SHIFT_GRID_X) & GRID_COORD_MASK;
        if (0 != (gKey >> SHIFT_GRID_X_SIGN))
        {
            gx *= -1;
        }

        int gy = (gKey >> SHIFT_GRID_Y) & GRID_COORD_MASK;
        if (0 != (gy & SHIFT_GRID_Y_SIGN))
        {
            gy *= -1;
        }

        int gz = (gKey >> SHIFT_GRID_Z) & GRID_COORD_MASK;
        if (0 != (gz & SHIFT_GRID_Z_SIGN))
        {
            gz *= -1;
        }

        return SIZE_GRID_AXIS * new float3(gx, gy, gz);
    }
    public static float3 GetTilePosition(int gKey, int tKey)
    {
        float3 grid_pivot = GetGridPosition(gKey);

        float tx = (tKey >> SHIFT_TILE_X) & TILE_COORD_MASK;
        float ty = (tKey >> SHIFT_TILE_Y) & TILE_COORD_MASK;
        float tz = (tKey >> SHIFT_TILE_Z) & TILE_COORD_MASK;

        return grid_pivot + new float3(tx, ty, tz);
    }

    public static bool TryGetTileData(ConcurrentDictionary<int, EditMapGridData> map, float3 position, out EditMapTileData tile_data)
    {
        int grid_key = GetGridKeyMask(position);
        int tile_key = GetTileKeyMask(position);

        if (false == map.ContainsKey(grid_key)
            || false == map[grid_key].TryGetTileData(tile_key, out tile_data))
        {
            tile_data = default;
            return false;
        }

        return true;
    }
}
#endif