namespace Script.Util
{
    using UnityEngine;

    public static class MapUtil
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
        private const int GRID_MAX_VALUE    = 0b_0011_1111; // == 63

        private const int SHIFT_TILE_Z      = 0;
        private const int SHIFT_TILE_Y      = 8;
        private const int SHIFT_TILE_X      = 16;
        private const int SHIFT_TILE_SMALL  = 24;
        private const int SHIFT_TILE_LAYER  = 25;

        /// <summary> 
        /// grid의 좌표는 각 축마다 [-127,127] 사이의 값을 가진다. <br/>
        /// scene_index를 부여하여 여러 씬을 사용할 수 있도록 하였다. <br/>
        /// scene[value_8], x[sign_1, value_7], y[sign_1, value_7], z[sign_1, value_7]
        /// </summary>
        public static int GetGridKeyMask(int sceneIndex, Vector3 gridPivot)
        {
            Vector3Int gridInt = gridPivot.ToInt();
            int gridFlag = 0;

            int x = gridInt.x;
            int y = gridInt.y;
            int z = gridInt.z;

            if (x < 0)
            {
                gridFlag |= 1 << SHIFT_GRID_X_SIGN;
                x *= -1;
            }
            gridFlag |= x << SHIFT_GRID_X;

            if (y < 0)
            {
                gridFlag |= 1 << SHIFT_GRID_Y_SIGN;
                y *= -1;
            }
            gridFlag |= y << SHIFT_GRID_Y;

            if (z < 0)
            {
                gridFlag |= 1 << SHIFT_GRID_Z_SIGN;
                z *= -1;
            }
            gridFlag |= z << SHIFT_GRID_Z;

            gridFlag |= sceneIndex << SHIFT_SCENE_INDEX;

            return gridFlag;
        }

        public static Vector3 GetGridPivot(Vector3 tilePivot)
        {
            float gx = Mathf.FloorToInt(tilePivot.x / GRID_MAX_VALUE);
            float gy = Mathf.FloorToInt(tilePivot.y / GRID_MAX_VALUE);
            float gz = Mathf.FloorToInt(tilePivot.z / GRID_MAX_VALUE);
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
        public static int GetTileKeyMask(Vector3 gridPivot, Vector3 tilePivot, int layer0to7, bool isSmall)
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
            mask |= layer0to7 << SHIFT_TILE_LAYER;

            return mask;
        }

        public static MapTileInfo GetTilePivot(this int key)
        {
            int x = (key >> SHIFT_TILE_X) & 0xFF;
            int y = (key >> SHIFT_TILE_Y) & 0xFF;
            int z = (key >> SHIFT_TILE_Z) & 0xFF;
            bool isSmall = (key & (1 << SHIFT_TILE_SMALL)) != 0;
            int layer = (key >> SHIFT_TILE_LAYER) & 0b_0000_0111; // 3 bits

            return new MapTileInfo(x, y, z, isSmall, layer);
        }

        public static Vector3 GetGridPivot(this int key)
        {
            int sceneIndex = (key >> SHIFT_SCENE_INDEX) & 0xFF;
            int x = (key >> SHIFT_GRID_X) & 0b_0111_1111; // 7 bits
            int y = (key >> SHIFT_GRID_Y) & 0b_0111_1111; // 7 bits
            int z = (key >> SHIFT_GRID_Z) & 0b_0111_1111; // 7 bits
            if ((key & (1 << SHIFT_GRID_X_SIGN)) != 0)
            {
                x *= -1;
            }
            if ((key & (1 << SHIFT_GRID_Y_SIGN)) != 0)
            {
                y *= -1;
            }
            if ((key & (1 << SHIFT_GRID_Z_SIGN)) != 0)
            {
                z *= -1;
            }
            return new Vector3(x, y, z);
        }

        public readonly struct MapTileInfo
        {
            public readonly Vector3 Pivot;
            public readonly int Layer;
            public readonly bool IsSmall;

            public MapTileInfo(int x, int y, int z, bool isSmall, int layer)
            {
                IsSmall = isSmall;
                Layer = layer;

                float size = isSmall ? 0.5f : 1f;
                Pivot = size * new Vector3(x, y, z);
            }

#if UNITY_EDITOR
            public void Debug(Vector3 gridPivot)
            {
                var pivot_coord = Pivot + gridPivot;
                UnityEngine.Debug.Log($"Tile Info: Pivot({pivot_coord.x:F0}, {pivot_coord.y:F0}, {pivot_coord.z:F0}), Layer: {Layer}, IsSmall: {IsSmall}");
            }
#endif
        }
    }
}
