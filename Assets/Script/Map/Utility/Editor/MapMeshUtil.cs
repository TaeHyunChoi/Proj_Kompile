namespace Script.Map.Utility
{
    using Script.Map.Data;
    using UnityEngine;
    using Unity.Mathematics;
    using System.Collections.Generic;

    public static class MapMeshUtil
    {
        public const float HeightStep = 0.125f;

        public static readonly float2[] PointCoords = new float2[]
        {
            new float2(0.0f, 0.0f),   new float2(0.5f, 0.0f),   new Vector2(1.0f, 0.0f),
            new float2(0.25f, 0.25f), new float2(0.75f, 0.25f),
            new float2(0.0f, 0.5f),   new float2(0.5f, 0.5f),   new float2(1.0f, 0.5f),
            new float2(0.25f, 0.75f), new float2(0.75f, 0.75f),
            new Vector2(0.0f, 1.0f),  new Vector2(0.5f, 1.0f),  new Vector2(1.0f, 1.0f)
        };

        public static readonly int[] TriangleIndices = new int[]
        {
            0, 3, 1,   1, 3, 6,   1, 6, 4,   1, 4, 2,
            0, 5, 3,   3, 5, 6,   4, 6, 7,   2, 4, 7,
            5, 8, 6,   6, 8, 11,  6, 11, 9,  6, 9, 7,
            5, 10, 8,  8, 10, 11, 9, 11, 12, 7, 9, 12
        };

        /// <summary>
        /// [수정됨] skipSideMask를 통해 보이지 않는 옆면을 생략합니다.
        /// </summary>
        public static Mesh GenerateMesh(MapTileHeightsData data, byte skipSideMask = 0)
        {
            Mesh mesh = new Mesh { name = "Generated3DBlockMesh" };

            Vector3[] vertices = new Vector3[45];
            Vector2[] uvs = new Vector2[45];
            List<int> dynamicTriangles = new List<int>();

            for (int i = 0; i < 13; i++)
            {
                sbyte h = data[i];
                float y = (h == -1) ? 0f : h * HeightStep;
                vertices[i] = new Vector3(PointCoords[i].x, y, PointCoords[i].y);
                uvs[i] = new Vector2(PointCoords[i].x, PointCoords[i].y);
            }

            for (int i = 0; i < TriangleIndices.Length; i += 3)
            {
                int v0 = TriangleIndices[i];
                int v1 = TriangleIndices[i + 1];
                int v2 = TriangleIndices[i + 2];
                if (data[v0] == -1 || data[v1] == -1 || data[v2] == -1) continue;
                dynamicTriangles.Add(v0); dynamicTriangles.Add(v1); dynamicTriangles.Add(v2);
            }

            int[] perimeter = { 0, 1, 2, 7, 12, 11, 10, 5 };
            int vIdx = 13;

            for (int i = 0; i < 8; i++)
            {
                int top1 = perimeter[i];
                int top2 = perimeter[(i + 1) % 8];

                // [핵심] 1. 정점이 삭제되었거나 2. 최적화 마스크에 의해 가려진 경우 옆면 생성 안 함
                bool isSideHidden = (skipSideMask & (1 << i)) != 0;
                if (data[top1] == -1 || data[top2] == -1 || isSideHidden)
                {
                    vIdx += 4;
                    continue;
                }

                vertices[vIdx] = vertices[top1];
                vertices[vIdx + 1] = vertices[top2];
                vertices[vIdx + 2] = new Vector3(vertices[top1].x, 0, vertices[top1].z);
                vertices[vIdx + 3] = new Vector3(vertices[top2].x, 0, vertices[top2].z);

                uvs[vIdx] = new Vector2(0, 1); uvs[vIdx + 1] = new Vector2(1, 1);
                uvs[vIdx + 2] = new Vector2(0, 0); uvs[vIdx + 3] = new Vector2(1, 0);

                dynamicTriangles.Add(vIdx); dynamicTriangles.Add(vIdx + 1); dynamicTriangles.Add(vIdx + 2);
                dynamicTriangles.Add(vIdx + 1); dynamicTriangles.Add(vIdx + 3); dynamicTriangles.Add(vIdx + 2);
                vIdx += 4;
            }

            mesh.vertices = vertices;
            mesh.triangles = dynamicTriangles.ToArray();
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}