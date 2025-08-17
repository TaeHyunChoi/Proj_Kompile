namespace Script.Util
{
    using UnityEngine;
    using Script.Data;

    // => MapTileInfo, Coordinate
    public static partial class MapUtil
    {
        // for on grid
        public static int SIZE_GRID_AXIS => GRID_MAX_VALUE;

        // _SIGN은 '부호(+/-) 플래그'
        private const int SHIFT_GRID_Z      = 0;
        private const int SHIFT_GRID_Z_SIGN = 7;
        private const int SHIFT_GRID_Y      = 8;
        private const int SHIFT_GRID_Y_SIGN = 15;
        private const int SHIFT_GRID_X      = 16;
        private const int SHIFT_GRID_X_SIGN = 23;
        private const int SHIFT_SCENE_INDEX = 24;
        private const int GRID_MAX_VALUE    = 64;
        private const int GRID_COORD_MASK   = 0b_0011_1111;

        private const int SHIFT_TILE_Z      = 0;
        private const int SHIFT_TILE_Y      = 8;
        private const int SHIFT_TILE_X      = 16;
        private const int SHIFT_TILE_SMALL  = 24;
        //private const int SHIFT_TILE_LAYER  = 25;

        /// <summary> 
        /// grid의 좌표는 각 축마다 [-127,127] 사이의 값을 가진다. <br/>
        /// scene_index를 부여하여 여러 씬을 사용할 수 있도록 하였다. <br/>
        /// scene[value_8], x[sign_1, value_7], y[sign_1, value_7], z[sign_1, value_7]
        /// </summary>
        public static int GetGridKeyMask(int sceneIndex, Vector3 gridPivot)
        {
            Vector3Int gridInt = gridPivot.ToInt();
            int sceneIndexMask = sceneIndex << SHIFT_SCENE_INDEX;
            int gridPivotMask = 0;

            int x = gridInt.x;
            int y = gridInt.y;
            int z = gridInt.z;

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
        public static Vector3 GetGridPivot(Vector3 position)
        {
            float gx = Mathf.FloorToInt(position.x / GRID_MAX_VALUE) * GRID_MAX_VALUE;
            float gy = Mathf.FloorToInt(position.y / GRID_MAX_VALUE) * GRID_MAX_VALUE;
            float gz = Mathf.FloorToInt(position.z / GRID_MAX_VALUE) * GRID_MAX_VALUE;
            return new Vector3(gx, gy, gz);
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
            Vector3Int diffInt = diff.ToInt();

            int mask = 0;
            mask |= (diffInt.z) << SHIFT_TILE_Z;
            mask |= (diffInt.y) << SHIFT_TILE_Y;
            mask |= (diffInt.x) << SHIFT_TILE_X;
            mask |= isSmall ? 1 << SHIFT_TILE_SMALL : 0;

            return mask;
        }

        public static MapTileInfo GetTilePivot(this int key)
        {
            int x = (key >> SHIFT_TILE_X) & 0xFF;
            int y = (key >> SHIFT_TILE_Y) & 0xFF;
            int z = (key >> SHIFT_TILE_Z) & 0xFF;
            bool isSmall = (key & (1 << SHIFT_TILE_SMALL)) != 0;
            //int layer = (key >> SHIFT_TILE_LAYER) & 0b_0000_0111; // 3 bits

            return new MapTileInfo(x, y, z, isSmall/*, layer*/);
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
            int sceneIndex = (key >> SHIFT_SCENE_INDEX) & 0xFF;

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

            return GRID_MAX_VALUE * new Vector3(x, y, z);
        }

        public readonly struct MapTileInfo
        {
            public readonly Vector3 Pivot;
            //public readonly int Layer;
            public readonly bool IsSmall;

            public MapTileInfo(int x, int y, int z, bool isSmall/*, int layer*/)
            {
                IsSmall = isSmall;
                //Layer = layer;

                float size = isSmall ? 0.5f : 1f;
                Pivot = size * new Vector3(x, y, z);
            }

#if UNITY_EDITOR
            public void Debug(Vector3 gridPivot)
            {
                var pivot_coord = Pivot + gridPivot;
                UnityEngine.Debug.Log($"Tile Info: Pivot({pivot_coord.x:F0}, {pivot_coord.y:F0}, {pivot_coord.z:F0}), IsSmall: {IsSmall}");
            }
#endif
        }
    }



    public static partial class MapUtil
    {
        private const float RADIUS = 0.5f;
        private static float[] rad = { RADIUS, RADIUS };

        private static Vector2[] triA  = new Vector2[64];
        private static Vector2[] triB  = new Vector2[64];
        private static Vector2[] triC  = new Vector2[64];
        private static Vector2[] tiles = new Vector2[4];

        public static void ScheduleOverlapCheck(MapGridData data, Vector3 next_position)
        {
            // 여기까지는 동기적으로 처리해도 된다?
            if (false == TryGetTargetTitles(data, next_position, out tiles))
            {
                return;
            }
            // 각 타일의 모든 삼각형을 가져오고 (16*4)
            // ScheduleOverlapCheck()로 넘겨서 판단하도록


            // 각 삼각형의 1번 꼭지점 모음
            // 각 삼각형의 2번 꼭지점 모음
            // 각 삼각형의 3번 꼭지점 모음

        }

        private static bool TryGetTargetTitles(MapGridData data, Vector3 position, out Vector2[] tilePivots)
        {
            tilePivots = null;

            // 해당 위치에 타일이 있는지부터 확인 좀;
            // 얘도 함수를 만들어야 하네?
            // 언제나 grid_pivot, tile_pivot을 구해야 하네

            // tile-pivot
            // isSmall 여부를 확인할 수 없음
            float tx = Mathf.CeilToInt(position.x);
            float ty = Mathf.CeilToInt(position.y);
            float tz = Mathf.CeilToInt(position.z);
            Vector3 tile_pivot = new Vector3(tx, ty, tz);

            var grid_pivot = GetGridPivot(tile_pivot);

            // 유효한 타일인지부터 확인 필요

            // position이 타일 내 어느 분면에 속해있는가
            int quadrant = GetQuadrant(tile_pivot - grid_pivot);
            switch (quadrant)
            {
                case 1:
                    tiles[0] = new Vector3(tx, ty, tz);
                    tiles[1] = new Vector3(tx, ty, tz);
                    tiles[2] = new Vector3(tx, ty, tz);
                    tiles[3] = new Vector3(tx, ty, tz);
                    break;
                case 2:
                    tiles[0] = new Vector3(tx, ty, tz);
                    tiles[1] = new Vector3(tx, ty, tz);
                    tiles[2] = new Vector3(tx, ty, tz);
                    tiles[3] = new Vector3(tx, ty, tz);
                    break;
                case 3:
                    tiles[0] = new Vector3(tx, ty, tz);
                    tiles[1] = new Vector3(tx, ty, tz);
                    tiles[2] = new Vector3(tx, ty, tz);
                    tiles[3] = new Vector3(tx, ty, tz);
                    break;
                case 4:
                    tiles[0] = new Vector3(tx, ty, tz);
                    tiles[1] = new Vector3(tx, ty, tz);
                    tiles[2] = new Vector3(tx, ty, tz);
                    tiles[3] = new Vector3(tx, ty, tz);
                    break;
            }

            return false;
        }

        private static int GetQuadrant(Vector3 position)
        {
            // 기준 길이가 1..
            float x = position.x;
            float z = position.z;

            // get tile pivot
            float px = Mathf.CeilToInt(x);
            float pz = Mathf.CeilToInt(z);

            bool bx = x - px >= 0.5f;
            bool bz = x - pz >= 0.5f;

            if ( bx &  bz) { return 1; }
            if ( bx & !bz) { return 4; }
            if (!bx &  bz) { return 2; }
            if (!bx & !bz) { return 3; }

            return 0;
        }
    }
}
