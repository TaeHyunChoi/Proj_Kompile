#if UNITY_EDITOR
namespace Study.Pathfind
{
    using Unity.Mathematics;
    using UnityEngine;

    public static class STUDY_PositionKeyUtil
    {
        public const int GRID_SIZE = 64;
        public const int TILE_BITS = 6;
        public const int TILE_MASK = (1 << TILE_BITS) - 1;

        public static long ComputeID(int gKey, int tKey)
        {
            return (((long)gKey) << 32) | (uint)tKey;
        }
        public static long ComputeID(Vector3 worldPos)
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
        public static int3 ComputeAbsoluteGridPivot(int gKey)
        {
            int gx = (sbyte)(byte)((gKey >> 16) & 0xFF);
            int gy = (sbyte)(byte)((gKey >> 8) & 0xFF);
            int gz = (sbyte)(byte)((gKey >> 0) & 0xFF);

            return GRID_SIZE * new int3(gx, gy, gz);
        }
    }
}
#endif
