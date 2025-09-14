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


    public static Vector3 GetGridPivot(Vector3 position, float rotY)
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
        Vector3 rotated;
        switch (rotInt)
        {
            case 90: rotated = new Vector3(0f, 0f, -1f); break;
            case 180: rotated = new Vector3(-1f, 0f, -1f); break;
            case 270: rotated = new Vector3(-1f, 0f, 0f); break;
            default: rotated = Vector3.zero; break;
        }

        position += rotated;
        float gx = Mathf.FloorToInt(position.x / SIZE_GRID_AXIS) * SIZE_GRID_AXIS;
        float gy = Mathf.FloorToInt(position.y / SIZE_GRID_AXIS) * SIZE_GRID_AXIS;
        float gz = Mathf.FloorToInt(position.z / SIZE_GRID_AXIS) * SIZE_GRID_AXIS;
        return new Vector3(gx, gy, gz);
    }
    /// <summary> 
    /// grid의 좌표는 각 축마다 [-127,127] 사이의 값을 가진다. <br/>
    /// scene_index를 부여하여 여러 씬을 사용할 수 있도록 하였다. <br/>
    /// scene[value_8], x[sign_1, value_7], y[sign_1, value_7], z[sign_1, value_7]
    /// </summary>
    public static int GetGridKeyMask(int sceneIndex, Vector3 gridPivot)
    {
        int sceneIndexMask = sceneIndex << SHIFT_SCENE_INDEX;
        int gridPivotMask = 0;

        int x = Mathf.RoundToInt(gridPivot.x);
        int y = Mathf.RoundToInt(gridPivot.y);
        int z = Mathf.RoundToInt(gridPivot.z);

        if (x < 0)
        {
            gridPivotMask |= 1 << SHIFT_GRID_X_SIGN;
            x *= -1;
        }
        gridPivotMask |= x << SHIFT_GRID_X;

        if (y < 0)
        {
            gridPivotMask |= 1 << SHIFT_GRID_Y_SIGN;
            y *= -1;
        }
        gridPivotMask |= y << SHIFT_GRID_Y;

        if (z < 0)
        {
            gridPivotMask |= 1 << SHIFT_GRID_Z_SIGN;
            z *= -1;
        }
        gridPivotMask |= z << SHIFT_GRID_Z;

        return sceneIndexMask | gridPivotMask;
    }
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
        int gx = Mathf.FloorToInt(position.x / SIZE_GRID_AXIS) * SIZE_GRID_AXIS;
        int gy = Mathf.FloorToInt(position.y / SIZE_GRID_AXIS) * SIZE_GRID_AXIS;
        int gz = Mathf.FloorToInt(position.z / SIZE_GRID_AXIS) * SIZE_GRID_AXIS;

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


    public static Vector3 GetTilePivot(Vector3 position, float rotY, bool isSmall)
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
        Vector3 rotated;
        switch (rotInt)
        {
            case 90: rotated = new Vector3(0f, 0f, -1f); break;
            case 180: rotated = new Vector3(-1f, 0f, -1f); break;
            case 270: rotated = new Vector3(-1f, 0f, 0f); break;
            default: rotated = Vector3.zero; break;
        }
        rotated *= isSmall ? 0.5f : 1f;
        return position + rotated;
    }
    /// <summary>
    /// grid_pivot으로부터 상대적인 거리를 계산하여 tile_pivot을 구한다. <br/>
    /// (grid가 parent object이고 tile이 child object라고 생각하자) <br/>
    /// nav[empty_4, layer_3, small_1], x[small_value_1, value_7], y[small_value_1, value_7], z[small_value_1, value_7] <br/>
    /// -- layer: 각 타일의 레이어를 나타낸다. <br/>
    /// -- small: 타일이 작은 크기인지 여부를 나타낸다. <br/>
    /// -- small_value_1: 작은 크기일 경우, 크기가 작아지므로 그만큼 더 많은 타일값을 저장해야 한다. 그래서 비워둔다. <br/>
    /// </summary>
    public static int GetTileKeyMask(Vector3 gridPivot, Vector3 tilePivot, bool isSmall)
    {
        Vector3 diff = tilePivot - gridPivot;
        if (true == isSmall)
        {
            diff *= 2f;
        }

        int x = Mathf.RoundToInt(diff.x);
        int y = Mathf.RoundToInt(diff.y);
        int z = Mathf.RoundToInt(diff.z);

        int mask = 0;
        mask |= z << SHIFT_TILE_Z;
        mask |= y << SHIFT_TILE_Y;
        mask |= x << SHIFT_TILE_X;
        mask |= isSmall ? 1 << SHIFT_TILE_SMALL : 0;

        return mask;
    }
    public static int GetTileKeyMask(float3 position)
    {
        int x = Mathf.RoundToInt(position.x % SIZE_GRID_AXIS);
        int y = Mathf.RoundToInt(position.y % SIZE_GRID_AXIS);
        int z = Mathf.RoundToInt(position.z % SIZE_GRID_AXIS);

        int tileKeyMask = 0;
        tileKeyMask |= x << SHIFT_TILE_X;
        tileKeyMask |= y << SHIFT_TILE_Y;
        tileKeyMask |= z << SHIFT_TILE_Z;

        return tileKeyMask;
    }
    public static Vector3 GetTilePivot(int gridKey, int tileKey)
    {
        Vector3 gridPivot = GetGridPivot(gridKey);

        int x = (tileKey >> SHIFT_TILE_X) & 0xFF;
        int y = (tileKey >> SHIFT_TILE_Y) & 0xFF;
        int z = (tileKey >> SHIFT_TILE_Z) & 0xFF;

        // tile key 에서 scale을 가져올 수 있잖아?
        int small_mask = (tileKey >> SHIFT_TILE_SMALL) & 1;
        float scale = small_mask != 0 ? 0.5f : 1f;

        return gridPivot + scale * new Vector3(x, y, z);
    }
    public static Vector3 GetGridPivot(int key)
    {
        //int sceneIndex = (key >> SHIFT_SCENE_INDEX) & 0xFF;

        int x = (key >> SHIFT_GRID_X) & GRID_COORD_MASK;
        int y = (key >> SHIFT_GRID_Y) & GRID_COORD_MASK;
        int z = (key >> SHIFT_GRID_Z) & GRID_COORD_MASK;

        if ((key & (1 << SHIFT_GRID_X_SIGN)) != 0)
        {
            x *= -1;
            x -= 1;
        }
        if ((key & (1 << SHIFT_GRID_Y_SIGN)) != 0)
        {
            y *= -1;
            y -= 1;
        }
        if ((key & (1 << SHIFT_GRID_Z_SIGN)) != 0)
        {
            z *= -1;
            z -= 1;
        }

        return SIZE_GRID_AXIS * new Vector3(x, y, z);
    }
}
#endif