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
            new float2(0.0f, 0.0f),   new float2(0.5f, 0.0f),   new float2(1.0f, 0.0f),
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
        /// [고도화] neighborHeights(길이 16)를 참조하여 맞닿는 구간만 부분 클리핑합니다.
        /// </summary>
        public static Mesh GenerateMesh(MapTileHeightsData data, sbyte[] neighborHeights = null)
        {
            Mesh mesh = new Mesh { name = "Generated3DBlockMesh" };

            Vector3[] vertices = new Vector3[45];
            Vector2[] uvs = new Vector2[45];
            List<int> dynamicTriangles = new List<int>();

            // 1. 윗면 생성
            for (int i = 0; i < 13; i++)
            {
                sbyte h = data[i];
                float y = (h == -1) ? 0f : h * HeightStep;
                vertices[i] = new Vector3(PointCoords[i].x, y, PointCoords[i].y);
                uvs[i] = new Vector2(PointCoords[i].x, PointCoords[i].y);
            }

            for (int i = 0; i < TriangleIndices.Length; i += 3)
            {
                int v0 = TriangleIndices[i]; int v1 = TriangleIndices[i + 1]; int v2 = TriangleIndices[i + 2];
                if (data[v0] == -1 || data[v1] == -1 || data[v2] == -1) continue;
                dynamicTriangles.Add(v0); dynamicTriangles.Add(v1); dynamicTriangles.Add(v2);
            }

            // 2. 옆면 가변 높이 클리핑
            int[] perimeter = { 0, 1, 2, 7, 12, 11, 10, 5 };
            int vIdx = 13;

            for (int i = 0; i < 8; i++)
            {
                int topIdx1 = perimeter[i];
                int topIdx2 = perimeter[(i + 1) % 8];

                sbyte h1 = data[topIdx1];
                sbyte h2 = data[topIdx2];

                if (h1 == -1 || h2 == -1) { vIdx += 4; continue; }

                // 이웃 타일 정점 높이 (배열 없으면 -1 취급)
                sbyte nh1 = (neighborHeights != null) ? neighborHeights[i * 2] : (sbyte)-1;
                sbyte nh2 = (neighborHeights != null) ? neighborHeights[i * 2 + 1] : (sbyte)-1;

                float floorY1 = (nh1 == -1) ? 0f : nh1 * HeightStep;
                float floorY2 = (nh2 == -1) ? 0f : nh2 * HeightStep;

                // 내 정점이 이웃 정점보다 같거나 낮으면 벽 생성 생략
                if (h1 <= nh1 && h2 <= nh2 && nh1 != -1 && nh2 != -1)
                {
                    vIdx += 4;
                    continue;
                }

                // 옆면 정점 4개: 윗면 높이부터 이웃 타일 높이(floorY)까지만 메쉬 생성
                vertices[vIdx] = vertices[topIdx1];
                vertices[vIdx + 1] = vertices[topIdx2];
                vertices[vIdx + 2] = new Vector3(vertices[topIdx1].x, floorY1, vertices[topIdx1].z);
                vertices[vIdx + 3] = new Vector3(vertices[topIdx2].x, floorY2, vertices[topIdx2].z);

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