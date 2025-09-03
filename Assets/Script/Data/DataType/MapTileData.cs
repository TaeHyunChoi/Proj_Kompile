namespace Script.Data
{
    using MessagePack;
    using UnityEngine;
    using static Index.MapTileIndex;

    [MessagePackObject]
    public struct MapTileData
    {
        [Key(0)]
        public long naviMask; // [layer:3bits], [heights:52bits(4*13)]

        [Key(1)]
        public uint infoMask;

        public MapTileData(long nav, uint info)
        {
            naviMask = nav;
            infoMask = info;
        }
    }

    public readonly struct IngameMapTileData
    {
        public readonly int GridKey;
        public readonly int TileKey;
        public readonly long NaviMask;
        public readonly uint InfoMask;

        public IngameMapTileData(int g, int t)
        {
            GridKey = g;
            TileKey = t;
            NaviMask = 0;
            InfoMask = 0;
        }
        public IngameMapTileData(int g, int t, MapTileData data)
        {
            GridKey = g;
            TileKey = t;
            NaviMask = data.naviMask;
            InfoMask = data.infoMask;
        }

        public Vector3 TilePosition
        {
            get
            {
                // grid_key to grid_parent_position
                int sign;
                int gx, gy, gz;

                sign = ((GridKey >> SHIFT_GRID_X_SIGN) & 1) == 0 ? 1 : -1;
                gx = sign * (GridKey >> SHIFT_GRID_X) & GRID_COORD_MASK;

                sign = ((GridKey >> SHIFT_GRID_Y_SIGN) & 1) == 0 ? 1 : -1;
                gy = sign * (GridKey >> SHIFT_GRID_Y) & GRID_COORD_MASK;

                sign = ((GridKey >> SHIFT_GRID_Z_SIGN) & 1) == 0 ? 1 : -1;
                gz = sign * (GridKey >> SHIFT_GRID_Z) & GRID_COORD_MASK;

                Vector3 gird_pivot = new Vector3(gx, gy, gz);

                // tile_key to tile_child_position
                int tx, ty, tz;

                tx = (TileKey >> SHIFT_TILE_X) & TILE_COORD_MASK;
                ty = (TileKey >> SHIFT_TILE_Y) & TILE_COORD_MASK;
                tz = (TileKey >> SHIFT_TILE_Z) & TILE_COORD_MASK;
                float scale = ((TileKey >> SHIFT_TILE_SMALL) & 1) > 0 ? 0.5f : 1;

                Vector3 tile_pivot = scale * new Vector3(tx, ty, tz);

                return gird_pivot + tile_pivot;
            }
        }
    }
}