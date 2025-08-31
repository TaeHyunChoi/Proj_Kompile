namespace Script.Util
{
    using UnityEngine;
    using Script.Data;
    using static Index.MapTileIndex;


    public static partial class MapUtil
    {
        public static (int, int) GetCoordKey(Vector3 position, bool isSmall)
        {
            Vector3Int grid_pivot_int = GetGridPivotPosition(position);
            int gridKey = PositionToGridMask(grid_pivot_int);

            // memo: small은 트리거로 바꾼다 치고, FieldManager.SmallScale; 식으로 들고 있어야할 듯
            Vector3 tile_pivot = GetTilePivotPosition(position, isSmall);
            int tileKey = PositionToTileMask(tile_pivot - grid_pivot_int, isSmall);

            return (gridKey, tileKey);
        }

        private static Vector3Int GetGridPivotPosition(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x / GRID_MAX_VALUE) * GRID_MAX_VALUE;
            int y = Mathf.FloorToInt(position.y / GRID_MAX_VALUE) * GRID_MAX_VALUE;
            int z = Mathf.FloorToInt(position.z / GRID_MAX_VALUE) * GRID_MAX_VALUE;

            return new Vector3Int(x, y, z);
        }
        private static int PositionToGridMask(Vector3Int pivotInt)
        {
            int gridKeyMask = 0;

            if (pivotInt.x < 0)
            {
                gridKeyMask |= 1 << SHIFT_GRID_X_SIGN;
                pivotInt.x *= -1;
            }
            gridKeyMask |= pivotInt.x << SHIFT_GRID_X;

            if (pivotInt.y < 0)
            {
                gridKeyMask |= 1 << SHIFT_GRID_Y_SIGN;
                pivotInt.y *= -1;
            }
            gridKeyMask |= pivotInt.y << SHIFT_GRID_Y;

            if (pivotInt.z < 0)
            {
                gridKeyMask |= 1 << SHIFT_GRID_Z_SIGN;
                pivotInt.z *= -1;
            }
            gridKeyMask |= pivotInt.z << SHIFT_GRID_Z;

            return gridKeyMask;
        }

        public static Vector3 GetTilePivotPosition(Vector3 position, bool isSmall)
        {
            float x, y, z;
            x = Mathf.Floor(position.x);
            y = Mathf.Floor(position.y);
            z = Mathf.Floor(position.z);

            // small-scale 타일은 0.5f 간격으로 pivot이 있음 (크기가 0.5f * 0.5f)
            // position의 소수점이 0.5f 이상이면 pivot은 ~.5f이고 반대라면 ~.0f가 pivot이 된다.
            if (true == isSmall)
            {
                float size = 0.5f;

                x += (position.x % 1f >= size) ? size : 0f;
                y += (position.y % 1f >= size) ? size : 0f;
                z += (position.z % 1f >= size) ? size : 0f;
            }

            return new Vector3(x, y, z);
        }

        private static int PositionToTileMask(Vector3 diff, bool isSmall)
        {
            int x = Mathf.RoundToInt(diff.x);
            int y = Mathf.RoundToInt(diff.y);
            int z = Mathf.RoundToInt(diff.z);

            if (true == isSmall)
            {
                x *= 2;
                y *= 2;
                z *= 2;
            }

            int mask = 0;
            mask |= x << SHIFT_TILE_X;
            mask |= y << SHIFT_TILE_Y;
            mask |= z << SHIFT_TILE_Z;

            return mask;
        }

        public static int GetQuarantInTile(Vector3 position, bool isSmall)
        {
            Vector3 tilePivot = GetTilePivotPosition(position, isSmall);
            Vector3 diff = position - tilePivot;

            float scale = (true == isSmall) ? 0.5f : 1f;
            float halfTileSize = 0.5f * scale;

            if (diff.z >= halfTileSize)
            {
                if (diff.x >= halfTileSize) { return 1; }
                else { return 2; }
            }
            else
            {
                if (diff.z >= halfTileSize) { return 4; }
                else { return 3; }
            }
        }

        public static Vector3 GetVertexPoint(int virtual_index, float y)
        {
            float x = 0f, z = 0f;
            switch (virtual_index)
            {
                case  0: x = 0.00f; z = 0.00f; break;
                case  1: x = 0.50f; z = 0.00f; break;
                case  2: x = 1.00f; z = 0.00f; break;
                case  3: x = 0.25f; z = 0.25f; break;
                case  4: x = 0.75f; z = 0.25f; break;
                case  5: x = 0.00f; z = 0.50f; break;
                case  6: x = 0.50f; z = 0.50f; break;
                case  7: x = 1.00f; z = 0.50f; break;
                case  8: x = 0.25f; z = 0.75f; break;
                case  9: x = 0.75f; z = 0.75f; break;
                case 10: x = 0.00f; z = 1.00f; break;
                case 11: x = 0.50f; z = 1.00f; break;
                case 12: x = 1.00f; z = 1.00f; break;
            }

            return new Vector3(x, y, z);
        }

        public static bool TryGetTrianglePoint(IngameMapTileData data, int tri_index, int pt_index, out Unity.Mathematics.float3 point)
        {
            int pt_virtual_index = TriangleVertex[tri_index * 3 + pt_index];
            int pt_height_mask = (int)((data.NaviMask >> pt_virtual_index * 4) & 0b_1111);

            // 유효하지 않은 point
            if (0x1000 < pt_height_mask)
            {
                point = default;
                return false;
            }

            float y = pt_height_mask * 0.125f;
            point = MapUtil.GetVertexPoint(pt_virtual_index, y);
            return true;
        }
    }
}
