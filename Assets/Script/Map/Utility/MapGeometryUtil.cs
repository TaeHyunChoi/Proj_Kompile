namespace Kompile.Map.Utility
{
    using Unity.Burst;
    using Unity.Mathematics;
    using Kompile.Map.Data;

    /// <summary>
    /// [Framework] Utility 계층
    /// 상태 없이 오직 기하학적 연산만 처리하는 순수 함수군. Burst Compile 최적화 적용.
    /// </summary>
    [BurstCompile]
    public static class MapGeometryUtil
    {
        /// <summary> 점 p가 삼각형 (a, b, c) 내부에 있는지 외적 부호로 판별합니다. </summary>
        [BurstCompile]
        private static bool IsPointInTriangle(in float2 p, in float2 a, in float2 b, in float2 c)
        {
            float cp1 = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
            float cp2 = (c.x - b.x) * (p.y - b.y) - (c.y - b.y) * (p.x - b.x);
            float cp3 = (a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x);

            return (cp1 >= 0f && cp2 >= 0f && cp3 >= 0f) || (cp1 <= 0f && cp2 <= 0f && cp3 <= 0f);
        }

        /// <summary> 삼각형 (a, b, c) 내 점 p의 바리센트릭 가중치 좌표를 계산합니다. </summary>
        [BurstCompile]
        private static void BarycentricCoords(in float2 p, in float2 a, in float2 b, in float2 c, out float3 result)
        {
            float2 v0 = b - a, v1 = c - a, v2 = p - a;
            float den = v0.x * v1.y - v1.x * v0.y;

            if (math.abs(den) < 1e-6f)
            {
                result = new float3(1f / 3f);
                return;
            }

            float v = (v2.x * v1.y - v1.x * v2.y) / den;
            float w = (v0.x * v2.y - v2.x * v0.y) / den;
            float u = 1f - v - w;

            result = new float3(u, v, w);
        }

        /// <summary> 
        /// 💡 [Burst] 특정 타일 데이터와 로컬 좌표를 기반으로 서브타일 삼각면 매핑 레이어를 고속 탐색하여 통과 가능 여부를 판별합니다. 
        /// </summary>
        [BurstCompile]
        public static bool IsTilePointWalkable(in MapTileData tile, in float2 localPos)
        {
            for (int s = 0; s < MapConsts.TRIANGLES_COUNT; s++)
            {
                int v0 = MapConsts.SubTileVertexMap[s * 3 + 0];
                int v1 = MapConsts.SubTileVertexMap[s * 3 + 1];
                int v2 = MapConsts.SubTileVertexMap[s * 3 + 2];

                float2 p0 = MapConsts.VertexPositions[v0];
                float2 p1 = MapConsts.VertexPositions[v1];
                float2 p2 = MapConsts.VertexPositions[v2];

                if (IsPointInTriangle(in localPos, in p0, in p1, in p2))
                {
                    return MapNaviTileUtil.IsSubTileValid(tile.NaviMask, s);
                }
            }
            return false;
        }

        /// <summary> 
        /// 💡 [Burst] 특정 타일 데이터 내 서브타일 삼각면 가중치를 보간하여 완벽한 3D 지형 높이를 역산합니다. 
        /// </summary>
        [BurstCompile]
        public static bool TrySampleTileHeight(in MapTileData tile, in float tileBaseY, in float2 localPos, in float heightStep, out float groundY)
        {
            groundY = tileBaseY;

            for (int s = 0; s < MapConsts.TRIANGLES_COUNT; s++)
            {
                if (!MapNaviTileUtil.IsSubTileValid(tile.NaviMask, s)) 
                    continue;                    
                
                int v0 = MapConsts.SubTileVertexMap[s * 3 + 0];
                int v1 = MapConsts.SubTileVertexMap[s * 3 + 1];
                int v2 = MapConsts.SubTileVertexMap[s * 3 + 2];

                float2 p0 = MapConsts.VertexPositions[v0];
                float2 p1 = MapConsts.VertexPositions[v1];
                float2 p2 = MapConsts.VertexPositions[v2];

                if (!IsPointInTriangle(in localPos, in p0, in p1, in p2)) 
                    continue;

                int h0 = MapNaviTileUtil.GetHeightFromNaviMask(tile.NaviMask, v0);
                int h1 = MapNaviTileUtil.GetHeightFromNaviMask(tile.NaviMask, v1);
                int h2 = MapNaviTileUtil.GetHeightFromNaviMask(tile.NaviMask, v2);

                BarycentricCoords(in localPos, in p0, in p1, in p2, out float3 bary);
                float sampledHeight = (bary.x * h0 + bary.y * h1 + bary.z * h2) * heightStep;

                groundY = tileBaseY + sampledHeight;
                return true;
            }
            return false;
        }

        /// <summary> 
        /// [Burst] 기준 방향 벡터와 회전 각도를 받아 XZ 평면 기준의 회전 변환된 방향 벡터를 초고속으로 계산합니다. 
        /// </summary>
        [BurstCompile]
        public static void CalculateArcDirection(in float3 moveDir, in float angle, out float3 result)
        {
            math.sincos(angle, out float sin, out float cos);
    
            // XZ 평면 회전 행렬 수식 적용 (baseDir -> moveDir로 교정)
            float rotX = moveDir.x * cos - moveDir.z * sin;
            float rotZ = moveDir.x * sin + moveDir.z * cos; 
    
            result = new float3(rotX, 0f, rotZ);
        }
    }
}