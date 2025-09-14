#if UNITY_EDITOR

using Unity.Mathematics;
using UnityEngine;
using static Script.Index.MapTileIndex;

/// <summary> 맵타일 관련하여 '에디터'에서만 사용하는 함수.
/// 비슷한 기능으로 함수가 많아지니까 헷갈려서 분리;
/// 개발하면서 함수 기능은 다시 정리하던가 그럽시다.
/// </summary>
public static class EditMapUtil
{
    public const int SPRITE_WIDTH = 256;
    public const int SPRITE_HEIGHT = 256;

    public const int TOTAL_BITS = 13;
    public const int BITS_PER_CELL = 4;
    public const int MATRIX_SIZE = 5;

    public static readonly Vector2Int[] INDEX_MAP = new Vector2Int[]
    {
            new Vector2Int(0, 4), new Vector2Int(2, 4), new Vector2Int(4, 4),
            new Vector2Int(1, 3), new Vector2Int(3, 3),
            new Vector2Int(0, 2), new Vector2Int(2, 2), new Vector2Int(4, 2),
            new Vector2Int(1, 1), new Vector2Int(3, 1),
            new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(4, 0)
    };

    /// <summary> 
    /// grid의 좌표는 각 축마다 [-127,127] 사이의 값을 가진다. <br/>
    /// scene_index를 부여하여 여러 씬을 사용할 수 있도록 하였다. <br/>
    /// scene[value_8], x[sign_1, value_7], y[sign_1, value_7], z[sign_1, value_7]
    /// </summary>
    public static int GetGridKeyMask(int sceneIndex, float3 position, float rotY)
    {
        // get: (rotated) pivot
        int rotInt = Mathf.RoundToInt(rotY);
        rotInt = (rotInt + 360) % 360;
        if (rotInt % 90 != 0)
        {
            Debug.LogError($"Tile has Wrong Rotation; ({rotInt})");
            return default;
        }

        // tile pivot : pivot 기준으로 회전을 시키면 pivot 좌표가 아래처럼 바뀐다는 뜻.
        float3 rotated;
        switch (rotInt)
        {
            case 90:    rotated = new float3( 0f, 0f, -1f); break;
            case 180:   rotated = new float3(-1f, 0f, -1f); break;
            case 270:   rotated = new float3(-1f, 0f,  0f); break;
            default:    rotated = new float3( 0f, 0f,  0f); break;
        }

        position += rotated;
        int gx = Mathf.FloorToInt(position.x / SIZE_GRID_AXIS);
        int gy = Mathf.FloorToInt(position.y / SIZE_GRID_AXIS);
        int gz = Mathf.FloorToInt(position.z / SIZE_GRID_AXIS);

        int sceneIndexMask = sceneIndex << SHIFT_SCENE_INDEX;
        int gridPivotMask = 0;

        if (gx < 0)
        {
            gridPivotMask |= 1 << SHIFT_GRID_X_SIGN;
            gx *= -1;
        }
        gridPivotMask |= gx << SHIFT_GRID_X;

        if (gy < 0)
        {
            gridPivotMask |= 1 << SHIFT_GRID_Y_SIGN;
            gy *= -1;
        }
        gridPivotMask |= gy << SHIFT_GRID_Y;

        if (gz < 0)
        {
            gridPivotMask |= 1 << SHIFT_GRID_Z_SIGN;
            gz *= -1;
        }
        gridPivotMask |= gz << SHIFT_GRID_Z;

        return sceneIndexMask | gridPivotMask;
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

        Debug.Log($"[Tile Key Mask]{System.Convert.ToString(tileKeyMask, 2)}");
        return tileKeyMask;
    }

    public static int3 GetGridPosition(int gKey)
    {
        int gx = (gKey >> SHIFT_GRID_X) & GRID_COORD_SIGNED_MASK;
        if (0 != (gx & GRID_SIGN_FLAG))
        {
            gx &= GRID_COORD_MASK;
            gx *= -1;
        }

        int gy = (gKey >> SHIFT_GRID_Y) & GRID_COORD_SIGNED_MASK;
        if (0 != (gx & GRID_SIGN_FLAG))
        {
            gy &= GRID_COORD_MASK;
            gy *= -1;
        }

        int gz = (gKey >> SHIFT_GRID_Z) & GRID_COORD_SIGNED_MASK;
        if (0 != (gz & GRID_SIGN_FLAG))
        {
            gz &= GRID_COORD_MASK;
            gz *= -1;
        }

        return new int3(gx, gy, gz);
    }
    public static int3 GetTilePosition(int gKey, int tKey)
    {
        int3 grid_pivot = GetGridPosition(gKey);

        int tx = (tKey >> SHIFT_TILE_X) & TILE_COORD_MASK;
        int ty = (tKey >> SHIFT_TILE_Y) & TILE_COORD_MASK;
        int tz = (tKey >> SHIFT_TILE_Z) & TILE_COORD_MASK;

        return grid_pivot + new int3(tx, ty, tz);
    }

    public static int PositionIntToGridKey(int3 positionInt)
    {
        int x = 0;
        if (positionInt.x < 0)
        {
            x |= 1 << SHIFT_GRID_X_SIGN;
            positionInt.x *= -1;
        }
        x |= (positionInt.x / 64) << SHIFT_GRID_X;

        int y = 0;
        if (positionInt.y < 0)
        {
            y |= 1 << SHIFT_GRID_Y_SIGN;
            positionInt.y *= -1;
        }
        y |= (positionInt.y / 64) << SHIFT_GRID_Y;

        int z = 0;
        if (positionInt.z < 0)
        {
            z |= 1 << SHIFT_GRID_Z_SIGN;
            positionInt.z *= -1;
        }
        z |= (positionInt.z / 64) << SHIFT_GRID_Z;


        return x | y | z;
    }
    public static int PositionIntToTileKey(int3 positionInt)
    {
        int x = (positionInt.x % 64) << SHIFT_TILE_X;
        int y = (positionInt.y % 64) << SHIFT_TILE_Y;
        int z = (positionInt.z % 64) << SHIFT_TILE_Z;

        return x | y | z;
    }
}
#endif