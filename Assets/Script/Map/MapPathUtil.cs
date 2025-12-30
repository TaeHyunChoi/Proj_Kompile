namespace Script.Map
{
    using Unity.Burst;
    using Unity.Mathematics;
    using UnityEngine;

    // 참고: STUDY_PositionKeyUtil
    public static class MapPathUtil
    {
        public const int GRID_SIZE = 64;
        public const int TILE_BITS = 6;
        public const int TILE_MASK = (1 << TILE_BITS) - 1;
        public const int NONE_SUBTILE = 0b1111;

        private static readonly int[] SubTileVertexMap = new int[]
        {
            0, 1, 3,    // s0
            1, 3, 6,    // s1
            3, 5, 6,    // s2
            0, 3, 5,    // s3
            1, 2, 4,    // s4
            2, 4, 7,    // s5
            4, 6, 7,    // s6
            1, 4, 6,    // s7
            5, 6, 8,    // s8
            6, 8, 11,   // s9
            8, 10, 11,  // s10
            5, 8, 10,   // s11
            6, 7, 9,    // s12
            7, 9, 12,   // s13
            9, 11, 12,  // s14
            6, 9, 11    // s15            
        };
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
        public static long ComputeTileID(float3 p)
        {
            const int GRID_SIZE = 64;
            const int TILE_BITS = 6;
            const int TILE_MASK = (1 << TILE_BITS) - 1;

            int absTx = Mathf.FloorToInt(p.x);
            int absTy = Mathf.FloorToInt(p.y);
            int absTz = Mathf.FloorToInt(p.z);

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

        [BurstCompile]
        public static bool IsSubTileValid(long naviMask, int sIndex0to15)
        {
            if (sIndex0to15 < 0 || sIndex0to15 > 15)
            {
                return false;
            }

            // 3개의 정점을 순회하며 유효성 체크
            for (int i = 0; i < 3; ++i)
            {
                int vIndex = SubTileVertexMap[sIndex0to15 * 3 + i];

                // 4비트씩 시프트하여 높이값 추출
                int vVal = (int)((naviMask >> (vIndex * 4)) & 0b1111);
                if (NONE_SUBTILE == vVal)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 월드 좌표를 서브 타일 인덱스(0..15)로 변환</br>
        /// tile.pivot 으로부터 상대 거리를 구해서 정점의 인덱스를 구하는 방식
        /// </summary>
        [BurstCompile]
        public static int GetSubTileIndex(float x, float z)
        {
            float localX = x - math.floor(x);
            float localZ = z - math.floor(z);

            // 사분면의 기준 인덱스:
            // 각 사분면의 서브 타일의 시작 인덱스는 (s0, s4, s8, s12)이다
            int col = (localX >= 0.5f) ? 1 : 0;
            int row = (localZ >= 0.5f) ? 1 : 0;
            int baseIndex = (row * 8) + (col * 4);

            // 사분면 내에서의 로컬 중심 좌표 계산:
            // 각 사분면은 0.5*0.5 크기이며 중심은 (0.25, 0.25)이다.
            float quadCenterX = (col * 0.5f) + 0.25f;
            float quadCenterZ = (row * 0.5f) + 0.25f;

            // 중심으로부터의 오차
            float dx = localX - quadCenterX;
            float dz = localZ - quadCenterZ;

            // (사분면의 중점으로부터 worldPos까지의 거리의 방향의) 절대값을 비교하여
            // 가로형(좌/우), 세로형(상/하)인지 판단
            int offset;
            if (math.abs(dx) > math.abs(dz))
            {
                // 가로형: dx가 양수면 오른쪽(right)
                offset = (dx > 0) ? 1 : 3;
            }
            else
            {
                // 세로형: dz가 양수면 위쪽(top)
                offset = (dz > 0) ? 2 : 0;
            }

            return baseIndex + offset;
        }
    }
}