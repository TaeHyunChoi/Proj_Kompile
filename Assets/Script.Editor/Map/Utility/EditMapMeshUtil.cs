#if UNITY_EDITOR
namespace Kompile.Map.Editor.Utility
{
    using Kompile.Map.Data;
    using UnityEngine;
    using Unity.Mathematics;
    using System.Collections.Generic;

    /// <summary> 데이터를 기반으로 실제 Unity Mesh를 직조합니다. </summary>
    public static class EditMapMeshUtil
    {
        public const float HeightStep = 0.125f; // 층간 고도 차이

        // 타일의 13개 정점 평면 좌표 (XZ 평면)
        public static readonly float2[] PointCoords = new float2[]
        {
            new float2(0.0f, 0.0f),   new float2(0.5f, 0.0f),   new float2(1.0f, 0.0f),
            new float2(0.25f, 0.25f), new float2(0.75f, 0.25f),
            new float2(0.0f, 0.5f),   new float2(0.5f, 0.5f),   new float2(1.0f, 0.5f),
            new float2(0.25f, 0.75f), new float2(0.75f, 0.75f),
            new Vector2(0.0f, 1.0f),  new Vector2(0.5f, 1.0f),  new Vector2(1.0f, 1.0f)
        };

        // 윗면(Top) 삼각형 직조를 위한 기본 인덱스 배열
        public static readonly int[] TriangleIndices = new int[]
        {
            0, 3, 1,   1, 3, 6,   1, 6, 4,   1, 4, 2,
            0, 5, 3,   3, 5, 6,   4, 6, 7,   2, 4, 7,
            5, 8, 6,   6, 8, 11,  6, 11, 9,  6, 9, 7,
            5, 10, 8,  8, 10, 11, 9, 11, 12, 7, 9, 12
        };

        // 방향성을 가진 엣지를 추가하거나, 이미 반대 방향이 있다면(공유된 선분) 서로 상쇄시킵니다.
        private static void AddDirectedEdge(HashSet<(int, int)> set, int from, int to)
        {
            if (set.Contains((to, from)))
            {
                set.Remove((to, from));
            }
            else
            {
                set.Add((from, to));
            }
        }

#if UNITY_EDITOR
        /// <summary> neighborHeights(길이 16)를 참조하여 맞닿는 구간만 부분 클리핑합니다.
        /// 이웃 타일과의 단차뿐만 아니라, 타일 내부에서 지워진 정점(-1)으로 인해 발생하는 모든 내부 절벽 단면까지 완벽하게 채워줍니다.
        /// </summary>
        public static Mesh GenerateMesh(MapTileHeightsData data, sbyte[] neighborHeights = null)
        {
            Mesh mesh = new Mesh { name = "Generated3DBlockMesh" };

            List<Vector3> vertices = new List<Vector3>(64);
            List<Vector2> uvs = new List<Vector2>(64);
            List<int> dynamicTriangles = new List<int>(64);

            // 1. 기본 13개 정점 윗면(Top) 위치 세팅
            for (int i = 0; i < 13; i++)
            {
                sbyte h = data[i];
                float y = (h == -1) ? 0f : h * HeightStep;  // height가 -1이면 0층 바닥으로 취급합니다.
                
                vertices.Add(new Vector3(PointCoords[i].x, y, PointCoords[i].y));
                uvs.Add(new Vector2(PointCoords[i].x, PointCoords[i].y));
            }

            // 노출된 외곽선(절벽 면)을 추출하기 위한 방향성 엣지 세트
            HashSet<(int, int)> exposedEdges = new HashSet<(int, int)>();

            // 2. 윗면(Top) 삼각형 직조
            for (int i = 0; i < TriangleIndices.Length; i += 3)
            {
                int v0 = TriangleIndices[i];
                int v1 = TriangleIndices[i + 1];
                int v2 = TriangleIndices[i + 2];

                // 버텍스 하나라도 지워져 있으면 해당 삼각형은 그리지 않습니다. (구멍 뚫기)
                if (data[v0] == -1 || data[v1] == -1 || data[v2] == -1)
                {
                    continue;
                }

                dynamicTriangles.Add(v0);
                dynamicTriangles.Add(v1);
                dynamicTriangles.Add(v2);

                // 그려진 삼각형의 외곽선을 방향성 있게 추가합니다. (공유된 선분은 상쇄됨)
                AddDirectedEdge(exposedEdges, v0, v1);
                AddDirectedEdge(exposedEdges, v1, v2);
                AddDirectedEdge(exposedEdges, v2, v0);
            }

            // 3. 단면(절벽 Side) 생성: 노출된 엣지를 바닥으로 내립니다.
            int[] perimeter = { 0, 1, 2, 7, 12, 11, 10, 5 }; // 타일 제일 바깥쪽 정점 순서

            // 상쇄되고 살아남은 엣지들은 모두 "절벽을 만들어야 할 노출된 단면"입니다.
            foreach (var edge in exposedEdges)
            {
                int idxA = edge.Item1; // 절벽 시작 버텍스 인덱스
                int idxB = edge.Item2; // 절벽 끝 버텍스 인덱스

                float topY1 = vertices[idxA].y; // 시작 버텍스 높이
                float topY2 = vertices[idxB].y; // 끝 버텍스 높이

                // 기본적으로 타일 내부 절벽은 y=0 바닥까지 떨어집니다. (나으리가 요구하신 하얀색 면)
                float floorY1 = 0f;
                float floorY2 = 0f;

                // 이 엣지가 타일의 바깥쪽 테두리(Perimeter)와 일치하는지 확인합니다.
                for (int p = 0; p < 8; p++)
                {
                    // 엣지의 반대 방향이 테두리 순서와 같다면, 그것은 외부 절벽입니다. (방향 트래킹 원리)
                    if (perimeter[p] == idxB && perimeter[(p + 1) % 8] == idxA)
                    {
                        // 외부 절벽은 바닥(y=0)이 아닌 이웃 타일의 높이까지만 벽을 내립니다. (클리핑)
                        sbyte nhB_h = (neighborHeights != null) ? neighborHeights[p * 2] : (sbyte)-1;
                        sbyte nhA_h = (neighborHeights != null) ? neighborHeights[p * 2 + 1] : (sbyte)-1;

                        floorY1 = (nhA_h == -1) ? 0f : nhA_h * HeightStep;
                        floorY2 = (nhB_h == -1) ? 0f : nhB_h * HeightStep;

                        // 내 타일보다 이웃 타일이 같거나 더 높으면 벽을 그릴 필요가 없습니다. (이웃 타일이 가려주므로)
                        if (data[idxA] <= nhA_h && data[idxB] <= nhB_h && nhA_h != -1 && nhB_h != -1)
                        {
                            floorY1 = float.MaxValue; // 벽 생성을 건너뛰기 위한 마커
                        }
                        break;
                    }
                }

                // 벽 생성 스킵
                if (floorY1 == float.MaxValue) 
                {
                    continue;
                }

                // 절벽 면(Quad)의 새 버텍스 인덱스 시작점
                int vIdx = vertices.Count;

                // A와 B의 윗면 높이에 해당하는 정점 2개 복제
                vertices.Add(new Vector3(PointCoords[idxA].x, topY1, PointCoords[idxA].y));
                vertices.Add(new Vector3(PointCoords[idxB].x, topY2, PointCoords[idxB].y));
                // A와 B의 바닥(또는 이웃 타일 높이)으로 수직 강하한 정점 2개 추가
                vertices.Add(new Vector3(PointCoords[idxA].x, floorY1, PointCoords[idxA].y));
                vertices.Add(new Vector3(PointCoords[idxB].x, floorY2, PointCoords[idxB].y));

                // 옆면 텍스처를 위한 UV 설정 (보는 방향 기준 Right, Left 매핑)
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));

                // 절벽 면의 삼각형 방향을 뒤집어 노말이 바깥을 향하게 직조합니다. (A -> B 방향 기준)
                dynamicTriangles.Add(vIdx); dynamicTriangles.Add(vIdx + 2); dynamicTriangles.Add(vIdx + 1);
                dynamicTriangles.Add(vIdx + 1); dynamicTriangles.Add(vIdx + 2); dynamicTriangles.Add(vIdx + 3);
            }

            // 4. 최종 메쉬 구성 및 최적화
            mesh.vertices = vertices.ToArray();
            mesh.triangles = dynamicTriangles.ToArray();
            mesh.uv = uvs.ToArray();

            // RecalculateNormals()가 뒤집힌 정점 방향에 맞춰 노말을 바깥쪽으로 예쁘게 뽑아줍니다.
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
#endif
    }
}
#endif