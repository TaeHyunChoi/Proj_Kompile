namespace Script.Util
{
    using Script.Data;
    using Unity.Mathematics;
    using UnityEngine;
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
                if (diff.x >= halfTileSize) { return 0; }
                else { return 1; }
            }
            else
            {
                if (diff.z >= halfTileSize) { return 3; }
                else { return 2; }
            }
        }

        public static bool TryGetTrianglePoint(IngameMapTileData data, int tri_index, int vertice, out Unity.Mathematics.float3 point)
        {
            int pt_virtual_index = TriangleVertex[tri_index * 3 + vertice];
            int pt_height_mask = (int)((data.NaviMask >> pt_virtual_index * 4) & 0b_1111);

            // 유효하지 않은 point
            if (0x1000 < pt_height_mask)
            {
                point = default;
                return false;
            }

            float x, z;
            float y = pt_height_mask * 0.125f;

            switch (pt_virtual_index)
            {
                case 0: x = 0.00f; z = 0.00f; break;
                case 1: x = 0.50f; z = 0.00f; break;
                case 2: x = 1.00f; z = 0.00f; break;
                case 3: x = 0.25f; z = 0.25f; break;
                case 4: x = 0.75f; z = 0.25f; break;
                case 5: x = 0.00f; z = 0.50f; break;
                case 6: x = 0.50f; z = 0.50f; break;
                case 7: x = 1.00f; z = 0.50f; break;
                case 8: x = 0.25f; z = 0.75f; break;
                case 9: x = 0.75f; z = 0.75f; break;
                case 10: x = 0.00f; z = 1.00f; break;
                case 11: x = 0.50f; z = 1.00f; break;
                case 12: x = 1.00f; z = 1.00f; break;
                default: x = 0.00f; z = 0.00f; break;
            }

            point = data.TilePosition + new Vector3(x, y, z);
            return true;
        }

        public static int GetTriangleIndex(Vector3 position, bool isSmall)
        {
            Vector3 tilePivot = MapUtil.GetTilePivotPosition(position, isSmall);
            Vector3 diff = position - tilePivot;

            float x = Mathf.Round(diff.x * 100f) * 0.01f;
            float z = Mathf.Round(diff.z * 100f) * 0.01f;

            int index = 0;
            index += (x >= 0.5f) ? 4 : 0;
            index += (z >= 0.5f) ? 8 : 0;

            x = Mathf.Round((x % 0.5f) * 100f) * 0.01f;
            z = Mathf.Round((z % 0.5f) * 100f) * 0.01f;

            bool zEx = z >= x;
            bool zEnx = z >= -x + 0.5f;

            if      (!zEx &  zEnx) { index += 1; }
            else if ( zEx &  zEnx) { index += 2; }
            else if ( zEx & !zEnx) { index += 3; }

            return index;
        }

        public static bool IsOverlaped(Vector3 pivot, float scale, Vector3 center)
        {
            float radius = scale * 0.25f;

            // 1. 원의 중심을 사각형에 가장 가까운 점으로 제한합니다.
            // 이 계산은 XZ 평면에서 이루어집니다.
            float closestX = Mathf.Clamp(center.x, pivot.x, pivot.x + scale);
            float closestZ = Mathf.Clamp(center.z, pivot.z, pivot.z + scale);

            // 2. 사각형의 가장 가까운 점을 기준으로 Vector3를 생성합니다.
            Vector3 closestPoint = new Vector3(closestX, pivot.y, closestZ);

            // 3. 가장 가까운 점과 원의 중심 사이의 거리를 계산합니다.
            // Vector3.Distance는 제곱근 연산을 포함하므로,
            // 성능을 위해 SqrMagnitude를 사용하여 제곱 거리로 비교하는 것이 더 효율적입니다.
            float distanceSquared = (closestPoint - center).sqrMagnitude;
            float radiusSquared = radius * radius;

            // 4. 거리가 반지름보다 작거나 같으면 겹치는 것입니다.
            return distanceSquared <= radiusSquared;
        }
        public static float CalculateYOnPlane(float3 a, float3 b, float3 c, float tx, float tz)
        {
            // 평면을 정의하는 두 벡터를 구합니다.
            float3 ab = b - a;
            float3 ac = c - a;

            // 두 벡터의 외적을 통해 평면의 법선 벡터를 구합니다.
            float3 normal = Vector3.Cross(ab, ac);

            // 평면의 방정식: A(x - x0) + B(y - y0) + C(z - z0) = 0
            // 법선 벡터의 성분이 A, B, C가 됩니다.
            float A = normal.x;
            float B = normal.y;
            float C = normal.z;

            // B가 0인 경우, 평면이 y축에 수직이므로 이 방법으로는 y값을 계산할 수 없습니다.
            // 이 경우, 사용자의 입력 또는 전제에 문제가 있을 수 있습니다.
            if (Mathf.Abs(B) < 1e-6)
            {
                Debug.LogError("The plane is parallel to the Y-axis. Cannot solve for y.");
                return float.NaN; // 유효하지 않은 값을 반환
            }

            // 평면의 방정식에 (tx, ty, tz)를 대입하고 ty에 대해 정리합니다.
            // A * (tx - a.x) + B * (ty - a.y) + C * (tz - a.z) = 0
            // B * (ty - a.y) = -A * (tx - a.x) - C * (tz - a.z)
            // ty - a.y = (-A * (tx - a.x) - C * (tz - a.z)) / B
            // ty = a.y + (-A * (tx - a.x) - C * (tz - a.z)) / B

            float ty = a.y + (-A * (tx - a.x) - C * (tz - a.z)) / B;
            return Mathf.RoundToInt(ty * 100f) * 0.01f;
        }
    }
}
