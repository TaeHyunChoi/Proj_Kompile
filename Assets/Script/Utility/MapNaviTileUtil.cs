namespace Kompile.Utility
{
    using Unity.Burst;
    using Unity.Mathematics;
    using static Kompile.Data.MapConsts;

    [BurstCompile]
    public static class MapNaviTileUtil
    {
        private const int NONE_SUBTILE = 0b1111;

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
                int vVal = (int)((naviMask >> (vIndex * 4)) & TILE_MASK);
                if (TILE_MASK == vVal)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// NaviMask에서 특정 정점(vIndex)의 4비트 높이 값을 추출합니다.
        /// </summary>
        [BurstCompile]
        public static int GetHeightFromNaviMask(long naviMask, int vIndex)
        {
            if (vIndex < 0 || vIndex > 12)
                return NONE_SUBTILE;

            return (int)((naviMask >> (vIndex * 4)) & 0b1111);
        }

        /// <summary>
        /// 타일 내 로컬 좌표(0~8)를 기반으로 Vertex Index(0~12)를 반환합니다.
        /// </summary>
        [BurstCompile]
        public static int GetVertexIndexFromLocalPos(int localX, int localZ)
        {
            // localX, localZ는 0 ~ 8 사이의 값 (0.125f 단위)
            switch ((localX, localZ))
            {
                // Bottom Line
                case (0, 0): return 0;
                case (4, 0): return 1;
                case (8, 0): return 2;

                // Bottom Quarter Line
                case (2, 2): return 3;
                case (6, 2): return 4;

                // Middle Line
                case (0, 4): return 5;
                case (4, 4): return 6;
                case (8, 4): return 7;

                // Top Quarter Line
                case (2, 6): return 8;
                case (6, 6): return 9;

                // Top Line
                case (0, 8): return 10;
                case (4, 8): return 11;
                case (2, 10): return 12;

                default:
                    return -1;
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

        [BurstCompile]
        public static bool IsCircleOverlappingSquare(in int3 pos, in float2 circleCenter, float radius)
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
        public static bool IsCircleOverlappingSubTile(int sIndex, in float2 circleCenter, float radiusSq)
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
                t = math.saturate(t);
                float2 closest = a + t * ab;

                return math.distancesq(p, closest);
            }
        }
    }
}