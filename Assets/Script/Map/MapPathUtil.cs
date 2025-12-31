namespace Script.Map
{
    using Unity.Burst;
    using Unity.Mathematics;
    using UnityEngine;

    public static class MapPathUtil
    {
        public const int GRID_SIZE = 64;
        public const int TILE_BITS = 6;
        public const int TILE_MASK = (1 << TILE_BITS) - 1;
        public const int NONE_SUBTILE = 0b1111;

        /// <summary> 서브 타일을 구성하는 3개의 정점 모음 </summary>
        public static readonly int[] SubTileVertexMap = new int[]
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

        /// <summary> 그림의 v00 ~ v12 위치를 2D 좌표로 매핑 </summary>
        public static readonly float2[] VertexPositions = new float2[]
        {
        new float2(0.00f, 0.00f), // v00
        new float2(0.50f, 0.00f), // v01
        new float2(1.00f, 0.00f), // v02
        new float2(0.25f, 0.25f), // v03 (Center of Bottom-Left Quad)
        new float2(0.75f, 0.25f), // v04 (Center of Bottom-Right Quad)
        new float2(0.00f, 0.50f), // v05
        new float2(0.50f, 0.50f), // v06 (Center of Tile)
        new float2(1.00f, 0.50f), // v07
        new float2(0.25f, 0.75f), // v08 (Center of Top-Left Quad)
        new float2(0.75f, 0.75f), // v09 (Center of Top-Right Quad)
        new float2(0.00f, 1.00f), // v10
        new float2(0.50f, 1.00f), // v11
        new float2(1.00f, 1.00f)  // v12
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
        public static long ComputeID(int gKey, int tKey)
        {
            const int SHFIT = 32;
            return ((long)gKey << SHFIT) | (uint)tKey;
        }
        public static Vector3 ComputeWorldPosition(long id)
        {
            int3 absPos = ComputeWorldPositionInt(id);
            return new Vector3(absPos.x, absPos.y, absPos.z);
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
                int vVal = (int)((naviMask >> (vIndex * 4)) & NONE_SUBTILE);
                if (NONE_SUBTILE == vVal)
                {
                    return false;
                }
            }

            return true;
        }
        [BurstCompile]
        public static bool IsCircleOverlappingSquare(int3 pos, float2 circleCenter, float radius)
        {
            const float TILE_SIZE = 1f;
            float2 squareMin = new float2(pos.x, pos.z);
            float2 squareMax = squareMin + new float2(TILE_SIZE, TILE_SIZE);

            // 1. 원의 중심에서 사각형 영역 안으로 가장 가까운 점(Closest Point)을 찾습니다.
            //    (삼각형 코드에서 변까지의 거리를 구하는 과정을 사각형에 맞춰 최적화한 것입니다.)
            float2 closestPoint = math.clamp(circleCenter, squareMin, squareMax);

            // 2. 그 '가장 가까운 점'과 '원의 중심' 사이의 거리를 구합니다.
            float distanceSq = math.distancesq(closestPoint, circleCenter);

            // 3. 거리의 제곱이 반지름의 제곱보다 작거나 같으면 겹친 것입니다.
            return distanceSq <= radius * radius;
        }
        [BurstCompile]
        public static bool IsCircleOverlappingSubTile(int sIndex, float2 circleCenter, float radiusSq)
        {
            int vIdx0 = SubTileVertexMap[sIndex * 3 + 0];
            int vIdx1 = SubTileVertexMap[sIndex * 3 + 1];
            int vIdx2 = SubTileVertexMap[sIndex * 3 + 2];

            float2 p0 = VertexPositions[vIdx0];
            float2 p1 = VertexPositions[vIdx1];
            float2 p2 = VertexPositions[vIdx2];

            // 원의 중심이 삼각형 '내부'에 있는지 먼저 확인
            if (true == IsPointInTriangle(circleCenter, p0, p1, p2))
            {
                // 내부에 있으면 무조건 겹침;
                return true;
            }

            // 삼각형 내부에 원이 없다면, 삼각형의 변(edge)와 원의 거리를 확인
            // 가장 가까운 변까지의 거리가 반지름보다 작으면 겹친 것

            float dSq0 = DistanceSqToSegment(p0, p1, circleCenter);
            if (dSq0 <= radiusSq)
            {
                return true;
            }

            float dSq1 = DistanceSqToSegment(p1, p2, circleCenter);
            if (dSq1 <= radiusSq)
            {
                return true;
            }

            float dSq2 = DistanceSqToSegment(p2, p0, circleCenter);
            if (dSq2 <= radiusSq)
            {
                return true;
            }

            return false;


            // 삼각형 내부에 있는가?
            static bool IsPointInTriangle(float2 p, float2 a, float2 b, float2 c)
            {
                // 2D 외적 (cross product) 부호 확인
                float cp1 = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
                float cp2 = (c.x - b.x) * (p.y - b.y) - (c.y - b.y) * (p.x - b.x);
                float cp3 = (a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x);

                return (cp1 >= 0 && cp2 >= 0 && cp3 >= 0) || (cp1 <= 0 && cp2 <= 0 && cp3 <= 0);
            }

            // 선분(a-b)과 점(p) 사이의 거리의 제곱을 반환
            static float DistanceSqToSegment(float2 a, float2 b, float2 p)
            {
                float2 ab = b - a;
                float2 ap = p - a;

                float t = math.dot(ap, ab) / math.dot(ab, ab);

                // 선분 범위를 벗어나지 않도록 0..1 사이로 Clamp
                t = math.saturate(t);

                // 가장 가까운 점
                float2 closest = a + t * ab;

                // 거리 제곱 반환
                return math.distancesq(p, closest);
            }
        }
        [BurstCompile]
        public static bool TryGetYInt(long linkMask, int dirIndex, out int yInt)
        {
            const int LINK_MASK = 0b11;
            int yMask = (int)(linkMask >> (dirIndex * 2)) & LINK_MASK;
            switch (yMask)
            {
                case 0b01: yInt = 0; break;
                case 0b10: yInt = 1; break;
                case 0b11: yInt = -1; break;
                default:
                    yInt = default;
                    return false;
            }

            return true;
        }
    }
}