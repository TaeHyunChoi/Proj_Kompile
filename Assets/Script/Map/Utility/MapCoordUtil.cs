namespace Script.Map.Utility
{
    using Script.Data;
    using Unity.Mathematics;
    using UnityEngine;

    public static class MapCoordUtil
    {
        private const int GRID_SIZE = 64;
        private const int TILE_MASK = (1 << TILE_BITS) - 1;
        private const int TILE_BITS = 6;

        /// <summary>
        /// 0.25f 단위의 정수 인덱스(indexX, indexY, indexZ)를 받아 TileID를 생성합니다.
        /// 부동소수점 연산과 분기문(if)을 제거하여 매우 빠릅니다.
        /// </summary>
        public static long ComputeTileIDInt(int3 pInt)
        {
            const int TILE_BITS = 6;
            const int TILE_MASK_VAL = (1 << TILE_BITS) - 1; // 63 (0x3F)

            // 1. 0.25f 단위 인덱스 -> 1.0f 단위 타일 좌표 변환
            // 설명: 0.25f는 1/4이므로 4로 나눕니다.
            // 비트 연산(>> 2)은 양수/음수 모두에서 '내림(Floor)'과 유사하게 작동하여 
            // Mathf.FloorToInt(worldPos)와 동일한 결과를 보장합니다.
            int absTx = pInt.x >> 3;
            int absTy = pInt.y >> 3;
            int absTz = pInt.z >> 3;

            // 2. 그룹(Chunk) 좌표 계산 (Grid Size = 64)
            // 64는 2^6이므로 >> 6 연산으로 나눗셈을 대체합니다.
            // 음수 좌표에서도 정확하게 그룹 인덱스를 찾아갑니다.
            int gX = absTx >> 6;
            int gY = absTy >> 6;
            int gZ = absTz >> 6;

            // 3. 그룹 내 로컬 타일 좌표 계산 (Modulo 64)
            // 기존 코드의 "tX = absTx - gX * GRID_SIZE" 및 "if (tX < 0)..." 로직 전체를 대체합니다.
            // 비트 마스크(& 63)는 음수에서도 항상 양수(0~63)인 순환 인덱스를 반환합니다.
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
        public static long ComputeID(int gKey, int tKey)
        {
            const int SHFIT = 32;
            return ((long)gKey << SHFIT) | (uint)tKey;
        }
        public static float3 ComputeWorldPosition(long id)
        {
            int3 absPos = ComputeWorldPositionInt(id);
            return new float3(absPos.x, absPos.y, absPos.z);
        }
        public static int3 ComputeWorldPositionInt(long id)
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
        public static void ComputeKey(float3 worldPos, out int outGKey, out int outTKey)
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

            outTKey = ((tX & TILE_MASK) << (TILE_BITS * 2))
                    | ((tY & TILE_MASK) << (TILE_BITS * 1))
                    | ((tZ & TILE_MASK) << (TILE_BITS * 0));

            byte bX = (byte)(sbyte)gX;
            byte bY = (byte)(sbyte)gY;
            byte bZ = (byte)(sbyte)gZ;

            outGKey = ((bX << 16) | (bY << 8) | bZ);
        }

        public static int ComputeGridKey(float3 worldPos)
        {
            int gx = Mathf.FloorToInt(worldPos.x / GRID_SIZE);
            int gy = Mathf.FloorToInt(worldPos.y / GRID_SIZE);
            int gz = Mathf.FloorToInt(worldPos.z / GRID_SIZE);

            byte bX = (byte)(sbyte)gx;
            byte bY = (byte)(sbyte)gy;
            byte bZ = (byte)(sbyte)gz;

            return (bX << 16) | (bY << 8) | bZ;
        }
        public static int ComputeGridKey(int gridKey, int3 offset)
        {
            int3 target = GetGridPivot(gridKey) + new int3(offset.x, offset.y, offset.z);

            byte bX = (byte)(sbyte)target.x;
            byte bY = (byte)(sbyte)target.y;
            byte bZ = (byte)(sbyte)target.z;

            return (bX << 16) | bY << 8 | bZ;
        }

        public static int3 GetGridPivot(int gridKey)
        {
            int x = (sbyte)((gridKey >> 16) & 0xFF);
            int y = (sbyte)((gridKey >> 8) & 0xFF);
            int z = (sbyte)((gridKey >> 0) & 0xFF);

            return new int3(x, y, z);
        }

        public static void ComputeKey(long id, out int outGKey, out int outTKey)
        {
            float3 position = ComputeWorldPosition(id);
            ComputeKey(position, out outGKey, out outTKey);
        }
        public static long ComputeTileID(Vector3 worldPos)
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
        public static int GetLinkMaskShift(EditMapTileDirFlag flag)
        {
            // 반시계 방향으로 돌린다~!!
            return 2 * flag switch
            {
                EditMapTileDirFlag.LEFT | EditMapTileDirFlag.DOWN => 0,
                EditMapTileDirFlag.DOWN => 1,
                EditMapTileDirFlag.RIGHT | EditMapTileDirFlag.DOWN => 2,
                EditMapTileDirFlag.RIGHT => 3,
                EditMapTileDirFlag.RIGHT | EditMapTileDirFlag.UP => 4,
                EditMapTileDirFlag.UP => 5,
                EditMapTileDirFlag.LEFT | EditMapTileDirFlag.UP => 6,
                EditMapTileDirFlag.LEFT => 7,
                _ => -1
            };
        }
    }
}