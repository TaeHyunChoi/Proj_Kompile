#if UNITY_EDITOR
namespace Script.Map.Utility
{
    using Script.Map.Data;
    using Unity.Mathematics;
    using UnityEngine;
    
    /// <summary>
    /// [Framework] Utility: 윗면 13포인트 + 옆면 벽(밑면 없음)으로 3D 블록을 생성합니다.
    /// </summary>
    public static class MapMeshUtil
    {
        public const float HeightStep = 0.125f;

        private static readonly float2[] PointCoords = new float2[]
        {
            new float2(0.0f, 0.0f),   new float2(0.5f, 0.0f),   new float2(1.0f, 0.0f),
            new float2(0.25f, 0.25f), new float2(0.75f, 0.25f),
            new float2(0.0f, 0.5f),   new float2(0.5f, 0.5f),   new float2(1.0f, 0.5f),
            new float2(0.25f, 0.75f), new float2(0.75f, 0.75f),
            new float2(0.0f, 1.0f),   new float2(0.5f, 1.0f),   new float2(1.0f, 1.0f)
        };

        private static readonly int[] TriangleIndices = new int[]
        {
            0, 3, 1,   1, 3, 6,   1, 6, 4,   1, 4, 2,
            0, 5, 3,   3, 5, 6,   4, 6, 7,   2, 4, 7,
            5, 8, 6,   6, 8, 11,  6, 11, 9,  6, 9, 7,
            5, 10, 8,  8, 10, 11, 9, 11, 12, 7, 9, 12
        };

        public static Mesh GenerateMesh(MapTileHeightsData data)
        {
            Mesh mesh = new Mesh { name = "Generated3DBlockMesh" };

            // 윗면 13개 + (옆면 8구간 * 4개 정점) = 총 45개 정점
            Vector3[] vertices = new Vector3[45];
            Vector2[] uvs = new Vector2[45];
            int[] triangles = new int[72 + 48]; // 윗면 삼각형 + 옆면 삼각형 개수

            // 1. 윗면 생성 (0 ~ 12번 정점)
            for (int i = 0; i < 13; i++)
            {
                float y = data[i] * HeightStep;
                vertices[i] = new Vector3(PointCoords[i].x, y, PointCoords[i].y);
                // 셰이더에서 WorldPosition을 쓸 거라 UV는 기본값만 넣습니다.
                uvs[i] = new Vector2(PointCoords[i].x, PointCoords[i].y);
            }

            for (int i = 0; i < TriangleIndices.Length; i++)
            {
                triangles[i] = TriangleIndices[i];
            }

            // 2. 옆면 생성 (밑면은 비워둠)
            // 윗면의 테두리를 이루는 정점 인덱스 순서 (시계방향)
            int[] perimeter = { 0, 1, 2, 7, 12, 11, 10, 5 };
            int vIdx = 13;
            int tIdx = 72;

            for (int i = 0; i < 8; i++)
            {
                int top1 = perimeter[i];
                int top2 = perimeter[(i + 1) % 8];

                Vector3 p1 = vertices[top1];
                Vector3 p2 = vertices[top2];

                // 각 벽면을 위한 독립된 4개의 정점 (모서리 각을 살리기 위해 분리)
                vertices[vIdx] = p1; // 좌상단
                vertices[vIdx + 1] = p2; // 우상단
                vertices[vIdx + 2] = new Vector3(p1.x, 0, p1.z); // 좌하단 (바닥 Y=0 고정)
                vertices[vIdx + 3] = new Vector3(p2.x, 0, p2.z); // 우하단 (바닥 Y=0 고정)

                uvs[vIdx] = new Vector2(0, 1); uvs[vIdx + 1] = new Vector2(1, 1);
                uvs[vIdx + 2] = new Vector2(0, 0); uvs[vIdx + 3] = new Vector2(1, 0);

                // 옆면 2개의 삼각형 연결
                triangles[tIdx++] = vIdx;
                triangles[tIdx++] = vIdx + 1;
                triangles[tIdx++] = vIdx + 2;

                triangles[tIdx++] = vIdx + 1;
                triangles[tIdx++] = vIdx + 3;
                triangles[tIdx++] = vIdx + 2;

                vIdx += 4;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;

            // 법선(Normal)을 재계산하여 빛을 제대로 받게 합니다.
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
#endif