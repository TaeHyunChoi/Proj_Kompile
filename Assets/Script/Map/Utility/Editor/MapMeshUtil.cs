#if UNITY_EDITOR
namespace Script.Map.Utility
{
    using Script.Map.Data;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// [Framework] Utility: 13개 포인트 데이터를 바탕으로 지형 메쉬를 계산하고 생성합니다.
    /// </summary>
    public static class MapMeshUtil
    {
        // 높이 1단계당의 실제 월드 Y값 (나으리의 규칙: 0.125 * n)
        public const float HeightStep = 0.125f;

        // [핵심 수정] 13개 포인트의 로컬 X, Z 좌표
        // 기존 -0.5 ~ 0.5 범위를 0.0 ~ 1.0 범위로 변경하여 Pivot(좌하단)과 일치시킵니다.
        private static readonly float2[] PointCoords = new float2[]
        {
            new float2(0.0f, 0.0f),   new float2(0.5f, 0.0f),   new float2(1.0f, 0.0f),   // 0: 좌하, 1: 중하, 2: 우하
            new float2(0.25f, 0.25f), new float2(0.75f, 0.25f),                            // 3: 내부좌하, 4: 내부우하
            new float2(0.0f, 0.5f),   new float2(0.5f, 0.5f),   new Vector2(1.0f, 0.5f),   // 5: 중좌, 6: 정중앙, 7: 중우
            new float2(0.25f, 0.75f), new float2(0.75f, 0.75f),                            // 8: 내부좌상, 9: 내부우상
            new float2(0.0f, 1.0f),   new float2(0.5f, 1.0f),   new Vector2(1.0f, 1.0f)    // 10: 좌상, 11: 중상, 12: 우상
        };

        // 삼각형 인덱스 구성 (이 부분은 점의 순서가 동일하므로 유지합니다)
        private static readonly int[] TriangleIndices = new int[]
        {
            0, 3, 1,   1, 3, 6,   1, 6, 4,   1, 4, 2,
            0, 5, 3,   3, 5, 6,   4, 6, 7,   2, 4, 7,
            5, 8, 6,   6, 8, 11,  6, 11, 9,  6, 9, 7,
            5, 10, 8,  8, 10, 11, 9, 11, 12, 7, 9, 12
        };

        /// <summary>
        /// 입력된 13개의 높이 데이터를 바탕으로 새로운 메쉬를 생성합니다.
        /// </summary>
        public static Mesh GenerateMesh(MapTileHeightsData data)
        {
            Mesh mesh = new Mesh { name = "GeneratedTileMesh_Dynamic" };

            Vector3[] vertices = new Vector3[13];
            Vector2[] uvs = new Vector2[13];

            for (int i = 0; i < 13; i++)
            {
                // 1. 높이 계산 (0.125 * n)
                float y = data[i] * HeightStep;

                // [확인] 이제 vertices[i].x와 z가 0~1 사이의 값을 가지므로 
                // Transform.position (Pivot) 기준으로 우측/상단 방향으로만 메쉬가 생성됩니다.
                vertices[i] = new Vector3(PointCoords[i].x, y, PointCoords[i].y);

                // 2. Base UV 계산
                // PointCoords가 이제 0~1 범위이므로 별도의 오프셋(+0.5f) 없이 그대로 사용 가능합니다.
                uvs[i] = new Vector2(PointCoords[i].x, PointCoords[i].y);
            }

            mesh.vertices = vertices;
            mesh.triangles = TriangleIndices;
            mesh.uv = uvs;

            // 라이팅 및 최적화를 위한 계산
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
#endif