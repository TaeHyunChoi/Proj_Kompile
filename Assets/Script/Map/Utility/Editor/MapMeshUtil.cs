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

        // 13개 포인트의 로컬 X, Z 좌표 (타일 중심 0,0 기준, 타일 크기 1x1 가정)
        private static readonly float2[] PointCoords = new float2[]
        {
            new float2(-0.5f, -0.5f), new float2(0.0f, -0.5f),  new float2(0.5f, -0.5f),  // 0: 좌하, 1: 중하, 2: 우하
            new float2(-0.25f, -0.25f), new float2(0.25f, -0.25f),                        // 3: 내부좌하, 4: 내부우하
            new float2(-0.5f, 0.0f),  new float2(0.0f, 0.0f),   new float2(0.5f, 0.0f),   // 5: 중좌, 6: 정중앙, 7: 중우
            new float2(-0.25f, 0.25f),  new float2(0.25f, 0.25f),                         // 8: 내부좌상, 9: 내부우상
            new float2(-0.5f, 0.5f),  new float2(0.0f, 0.5f),   new float2(0.5f, 0.5f)    // 10: 좌상, 11: 중상, 12: 우상
        };

        // 이미지의 선을 기반으로 구성한 삼각형 인덱스들
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
                vertices[i] = new Vector3(PointCoords[i].x, y, PointCoords[i].y);

                // 2. Base UV 계산 (-0.5~0.5 범위의 로컬 좌표를 0~1로 변환)
                // 이 UV 값은 이후 컴포넌트의 OnValidate에서 MaterialPropertyBlock을 통해 재조정됩니다.
                uvs[i] = new Vector2(PointCoords[i].x + 0.5f, PointCoords[i].y + 0.5f);
            }

            mesh.vertices = vertices;
            mesh.triangles = TriangleIndices;
            mesh.uv = uvs;

            // 라이팅 및 충돌(Culling)을 위한 계산
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
#endif