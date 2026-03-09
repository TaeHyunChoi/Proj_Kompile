namespace Script.Map.Utility
{
    using Unity.Burst;
    using Unity.Mathematics;
    using Script.Map.Data;
    using static Script.Map.Data.MapConsts;

    [BurstCompile]
    public static class MapNaviTileUtil
    {
        [BurstCompile]
        public static bool IsSubTileValid(long naviMask, int sIndex0to15)
        {
            if (sIndex0to15 < 0 || sIndex0to15 > 15)
            {
                return false;
            }

            // 3개의 정점을 순회하며 유효성 체크
            int NONE_SUBTILE = 0b1111;
            for (int i = 0; i < 3; ++i)
            {
                int vIndex = MapConsts.SubTileVertexMap[sIndex0to15 * 3 + i];

                // 4비트씩 시프트하여 높이값 추출
                int vVal = (int)((naviMask >> (vIndex * 4)) & NONE_SUBTILE);
                if (NONE_SUBTILE == vVal)
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
            if (vIndex < 0 || vIndex > 12) return 15; // Error or None
            return (int)((naviMask >> (vIndex * 4)) & 0b1111);
        }
        
        /// <summary>
        /// 타일 내 로컬 좌표(0~8)를 기반으로 Vertex Index(0~12)를 반환합니다.
        /// </summary>
        [BurstCompile]
        public static int GetVertexIndexFromLocalPos(int localX, int localZ)
        {
            // localX, localZ는 0 ~ 8 사이의 값 (0.125f 단위)

            if (localZ == 0) // Bottom Line
            {
                if (localX == 0) return 0; // v00
                if (localX == 4) return 1; // v01
                if (localX == 8) return 2; // v02
            }
            else if (localZ == 2) // Bottom Quarter Line
            {
                if (localX == 2) return 3; // v03
                if (localX == 6) return 4; // v04
            }
            else if (localZ == 4) // Middle Line
            {
                if (localX == 0) return 5; // v05
                if (localX == 4) return 6; // v06
                if (localX == 8) return 7; // v07
            }
            else if (localZ == 6) // Top Quarter Line
            {
                if (localX == 2) return 8; // v08
                if (localX == 6) return 9; // v09
            }
            else if (localZ == 8) // Top Line
            {
                if (localX == 0) return 10; // v10
                if (localX == 4) return 11; // v11
                if (localX == 8) return 12; // v12
            }

            return -1; // 유효한 정점 위치가 아님
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
            int vIdx0 = MapConsts.SubTileVertexMap[sIndex * 3 + 0];
            int vIdx1 = MapConsts.SubTileVertexMap[sIndex * 3 + 1];
            int vIdx2 = MapConsts.SubTileVertexMap[sIndex * 3 + 2];

            float2 p0 = MapConsts.VertexPositions[vIdx0];
            float2 p1 = MapConsts.VertexPositions[vIdx1];
            float2 p2 = MapConsts.VertexPositions[vIdx2];

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
                #if UNITY_EDITOR
        public static EditMapTileDirFlag GetDirFlag(float x, float z)
        {
            EditMapTileDirFlag flag = EditMapTileDirFlag.NONE;

            if (x > 0) { flag |= EditMapTileDirFlag.RIGHT; }
            else if (x < 0) { flag |= EditMapTileDirFlag.LEFT; }

            if (z > 0) { flag |= EditMapTileDirFlag.UP; }
            else if (z < 0) { flag |= EditMapTileDirFlag.DOWN; }

            return flag;
        }
        public static EditVertexContextData GetVertexIndexInfo(EditMapTileDirFlag flag)
        {
            return flag switch
            {
                EditMapTileDirFlag.LEFT => new EditVertexContextData(5, 0, 10),
                EditMapTileDirFlag.RIGHT => new EditVertexContextData(7, 2, 12),
                EditMapTileDirFlag.UP => new EditVertexContextData(11, 10, 12),
                EditMapTileDirFlag.DOWN => new EditVertexContextData(1, 0, 2),
                _ => default
            };
        }
        public static float3 GetDirectionVector(EditMapTileDirFlag flag)
        {
            return flag switch
            {
                EditMapTileDirFlag.LEFT => new float3(-1f, 0f, 0f),
                EditMapTileDirFlag.RIGHT => new float3(1f, 0f, 0f),
                EditMapTileDirFlag.UP => new float3(0f, 0f, 1f),
                EditMapTileDirFlag.DOWN => new float3(0f, 0f, -1f),
                _ => default
            };
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
        
        [BurstCompile]
        public static bool TryGetVerticeHeight(this in EditMapTileData data, int vertice, out int heightx1000)
        {
            // data가 'in'이므로 필드 접근 시 복사가 일어나지 않는다.
            int shift = MapConsts.HEIGHT_BITS * vertice;
            int maskInt = (int)((data.NaviMask >> shift) & MapConsts.HEIGHT_MASK);

            if (maskInt == 0b1111)
            {
                heightx1000 = 0;
                return false;
            }

            MapCoordUtil.ComputeWorldPosition(data.ID, out float3 pos);
            float pivotY = pos.y;
            float actualHeight = pivotY + (maskInt * 0.125f);
    
            heightx1000 = (int)(actualHeight * 1000f + 0.5f); 
            return true;
        }
#endif
    }
}