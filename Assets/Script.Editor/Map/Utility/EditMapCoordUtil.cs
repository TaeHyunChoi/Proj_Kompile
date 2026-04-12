#if UNITY_EDITOR
namespace Kompile.Map.Editor.Utility
{
    using Unity.Mathematics;
    using UnityEngine;
    using Unity.Burst;

    [BurstCompile]
    public static class EditMapCoordUtil
    {
        private const int GRID_SIZE = 64;
        private const int TILE_MASK = (1 << TILE_BITS) - 1;
        private const int TILE_BITS = 6;

        private const float GRID_SIZE_RECIP = 1f / GRID_SIZE;

        /// <summary>
        /// 0.25f 단위의 정수 인덱스(indexX, indexY, indexZ)를 받아 TileID를 생성합니다.
        /// 부동소수점 연산과 분기문(if)을 제거하여 매우 빠릅니다.
        /// </summary>
        public static long ComputeTileIDInt(in int3 pInt)
        {
            const int TILE_BITS = 6;
            const int TILE_MASK_VAL = (1 << TILE_BITS) - 1; // 63 (0x3F)

            // 1. 0.25f 단위 인덱스 -> 1.0f 단위 타일 좌표 변환
            int absTx = pInt.x >> 3;
            int absTy = pInt.y >> 3;
            int absTz = pInt.z >> 3;

            // 2. 그룹(Chunk) 좌표 계산 (Grid Size = 64)
            int gX = absTx >> 6;
            int gY = absTy >> 6;
            int gZ = absTz >> 6;

            // 3. 그룹 내 로컬 타일 좌표 계산 (Modulo 64)
            int tX = absTx & TILE_MASK_VAL;
            int tY = absTy & TILE_MASK_VAL;
            int tZ = absTz & TILE_MASK_VAL;

            // 4. 비트 패킹 (기존 로직과 동일)
            uint tKey =
                (uint)((tX & TILE_MASK_VAL) << (TILE_BITS * 2)) |
                (uint)((tY & TILE_MASK_VAL) << (TILE_BITS * 1)) |
                (uint)((tZ & TILE_MASK_VAL) << (TILE_BITS * 0));

            // Group 좌표 패킹 (byte 캐스팅으로 범위 초과 시 자동 순환)
            byte bX = (byte)gX;
            byte bY = (byte)gY;
            byte bZ = (byte)gZ;

            uint gKey = (uint)((bX << 16) | (bY << 8) | bZ);

            return (((long)gKey) << 32) | tKey;
        }

        [BurstCompile]
        public static void ComputeID(int gKey, int tKey, out long outID)
        {
            const int SHFIT = 32;
            outID = ((long)gKey << SHFIT) | (uint)tKey;
        }

        public static void ComputeWorldPosition(long id, out float3 outWorldPos)
        {
            ComputeWorldPositionInt(id, out int3 absPos);
            outWorldPos = new float3(absPos.x, absPos.y, absPos.z);
        }

        [BurstCompile]
        public static void ComputeWorldPositionInt(long id, out int3 outWorldPosInt)
        {
            uint gKey = (uint)((ulong)id >> 32);
            uint tKey = (uint)id;

            int gx = (sbyte)(byte)((gKey >> 16) & 0xFF);
            int gy = (sbyte)(byte)((gKey >> 8) & 0xFF);
            int gz = (sbyte)(byte)((gKey >> 0) & 0xFF);

            int tx = (int)((tKey >> (TILE_BITS * 2)) & TILE_MASK);
            int ty = (int)((tKey >> (TILE_BITS * 1)) & TILE_MASK);
            int tz = (int)((tKey >> (TILE_BITS * 0)) & TILE_MASK);

            outWorldPosInt = new int3(
                gx * GRID_SIZE + tx,
                gy * GRID_SIZE + ty,
                gz * GRID_SIZE + tz);
        }

        [BurstCompile]
        public static void ComputeKey(in float3 worldPos, out int outGKey, out int outTKey)
        {
            int absTx = Mathf.FloorToInt(worldPos.x);
            int absTy = Mathf.FloorToInt(worldPos.y);
            int absTz = Mathf.FloorToInt(worldPos.z);

            int gX = Mathf.FloorToInt((float)absTx * GRID_SIZE_RECIP);
            int gY = Mathf.FloorToInt((float)absTy * GRID_SIZE_RECIP);
            int gZ = Mathf.FloorToInt((float)absTz * GRID_SIZE_RECIP);

            int tX = absTx - gX * GRID_SIZE;
            int tY = absTy - gY * GRID_SIZE;
            int tZ = absTz - gZ * GRID_SIZE;

            if (tX < 0)
            {
                tX += GRID_SIZE;
                gX -= 1;
            }

            if (tY < 0)
            {
                tY += GRID_SIZE;
                gY -= 1;
            }

            if (tZ < 0)
            {
                tZ += GRID_SIZE;
                gZ -= 1;
            }

            outTKey = ((tX & TILE_MASK) << (TILE_BITS * 2))
                      | ((tY & TILE_MASK) << (TILE_BITS * 1))
                      | ((tZ & TILE_MASK) << (TILE_BITS * 0));

            byte bX = (byte)(sbyte)gX;
            byte bY = (byte)(sbyte)gY;
            byte bZ = (byte)(sbyte)gZ;

            outGKey = ((bX << 16) | (bY << 8) | bZ);
        }

        [BurstCompile]
        public static int ComputeGridKey(in float3 worldPos)
        {
            int gx = Mathf.FloorToInt(worldPos.x * GRID_SIZE_RECIP);
            int gy = Mathf.FloorToInt(worldPos.y * GRID_SIZE_RECIP);
            int gz = Mathf.FloorToInt(worldPos.z * GRID_SIZE_RECIP);

            byte bX = (byte)(sbyte)gx;
            byte bY = (byte)(sbyte)gy;
            byte bZ = (byte)(sbyte)gz;

            return (bX << 16) | (bY << 8) | bZ;
        }

        [BurstCompile]
        public static void ComputeKey(long id, out int outGKey, out int outTKey)
        {
            ComputeWorldPosition(id, out float3 position);
            ComputeKey(position, out outGKey, out outTKey);
        }

        [BurstCompile]
        public static long ComputeTileID(in float3 worldPos)
        {
            int absTx = Mathf.FloorToInt(worldPos.x);
            int absTy = Mathf.FloorToInt(worldPos.y);
            int absTz = Mathf.FloorToInt(worldPos.z);

            int gX = Mathf.FloorToInt((float)absTx * GRID_SIZE_RECIP);
            int gY = Mathf.FloorToInt((float)absTy * GRID_SIZE_RECIP);
            int gZ = Mathf.FloorToInt((float)absTz * GRID_SIZE_RECIP);

            int tX = absTx - gX * GRID_SIZE;
            int tY = absTy - gY * GRID_SIZE;
            int tZ = absTz - gZ * GRID_SIZE;

            if (tX < 0)
            {
                tX += GRID_SIZE;
                gX -= 1;
            }

            if (tY < 0)
            {
                tY += GRID_SIZE;
                gY -= 1;
            }

            if (tZ < 0)
            {
                tZ += GRID_SIZE;
                gZ -= 1;
            }

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

        [BurstCompile]
        public static void GetPivot(int gKey, int tKey, out float3 pivot)
        {
            long id = ((long)gKey << 32) | (uint)tKey;
            ComputeWorldPositionInt(id, out int3 absPos);
            pivot = new float3(absPos.x, absPos.y, absPos.z);
        }
    }
}
#endif