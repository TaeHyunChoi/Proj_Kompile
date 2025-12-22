namespace Script.Map
{
    using Unity.Mathematics;
    using UnityEngine;

    // 참고: STUDY_PositionKeyUtil
    public static class MapPathUtil
    {
        public const int GRID_SIZE = 64;
        public const int TILE_BITS = 6;
        public const int TILE_MASK = (1 << TILE_BITS) - 1;

        public static long ComputeID(int gKey, int tKey)
        {
            const int SHFIT = 32;
            return ((long)gKey << SHFIT) | (uint)tKey;
        }
        public static Vector3 ComputeWorldPosition(long id)
        {
            int3 absPos = ComputeAbsoluteWorldPosition(id);
            return new Vector3(absPos.x, absPos.y, absPos.z);
        }
        public static int3 ComputeAbsoluteWorldPosition(long id)
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

    }
}