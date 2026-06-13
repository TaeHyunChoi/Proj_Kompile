namespace Kompile.Map.Utility
{
    using Unity.Burst;
    using Unity.Mathematics;

    /// <summary>
    /// [Framework] Utility 계층
    /// 상태 없이 오직 기하학적 연산만 처리하는 순수 함수군. Burst Compile 최적화 적용.
    /// </summary>
    [BurstCompile]
    public static class MapGeometryUtil
    {
        /// <summary> 점 p가 삼각형 (a, b, c) 내부에 있는지 외적 부호로 판별합니다. </summary>
        [BurstCompile]
        public static bool IsPointInTriangle(float2 p, float2 a, float2 b, float2 c)
        {
            float cp1 = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
            float cp2 = (c.x - b.x) * (p.y - b.y) - (c.y - b.y) * (p.x - b.x);
            float cp3 = (a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x);

            return (cp1 >= 0f && cp2 >= 0f && cp3 >= 0f) || (cp1 <= 0f && cp2 <= 0f && cp3 <= 0f);
        }

        /// <summary> 삼각형 (a, b, c) 내 점 p의 바리센트릭 가중치 좌표를 계산합니다. </summary>
        [BurstCompile]
        public static float3 BarycentricCoords(float2 p, float2 a, float2 b, float2 c)
        {
            float2 v0 = b - a, v1 = c - a, v2 = p - a;
            float den = v0.x * v1.y - v1.x * v0.y;

            if (math.abs(den) < 1e-6f)
            {
                return new float3(1f / 3f);
            }

            float v = (v2.x * v1.y - v1.x * v2.y) / den;
            float w = (v0.x * v2.y - v2.x * v0.y) / den;
            float u = 1.0f - v - w;

            return new float3(u, v, w);
        }
    }
}