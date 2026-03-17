namespace Script.Map.Utility
{
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// [Framework] Utility: 상태 없이 길찾기 계산만 수행하는 순수 함수군입니다.
    /// Burst Compile 최적화를 위해 Native 자료구조만 매개변수로 받으며, 데이터의 소유권을 가지지 않습니다.
    /// </summary>
    public static class AStarPathfinderUtil
    {
        /// <summary>
        /// 여러 출발지/목적지에 대한 A* 길찾기를 Job System을 통해 병렬로 수행합니다.
        /// </summary>
        /// <param name="starts">출발지 목록</param>
        /// <param name="ends">목적지 목록</param>
        /// <param name="nativeMap">호출부(Manager 등)에서 전달하는 네이티브 맵 데이터 (런타임/에디터 환경에 맞춰 주입됨)</param>
        public static List<float3[]> RequestPathsBatch(
            List<Vector3> starts, 
            List<Vector3> ends,
            NativeHashMap<long, (long, long)> nativeMap)
        {
            int batchCount = starts.Count;
            var nativeStarts = new NativeArray<float3>(batchCount, Allocator.TempJob);
            var nativeEnds = new NativeArray<float3>(batchCount, Allocator.TempJob);
            var resultStream = new NativeStream(batchCount, Allocator.TempJob);

            for (int i = 0; i < batchCount; i++)
            {
                nativeStarts[i] = starts[i];
                nativeEnds[i] = ends[i];
            }

            // [수정됨] EditMapRepoProvider 호출이 삭제되었습니다. 
            // 주입받은 nativeMap을 바로 Job에 전달하여 어드레서블 빌드 시 에디터 스크립트 포함 문제를 해결합니다.
            var job = new AStarBatchJobUtil
            {
                StartPositions = nativeStarts,
                EndPositions = nativeEnds,
                Radius = 0.325f,
                Map = nativeMap,
                ResultPathStream = resultStream.AsWriter()
            };

            JobHandle handle = job.Schedule(batchCount, 1);
            handle.Complete();

            List<float3[]> finalPaths = new List<float3[]>(batchCount);
            NativeStream.Reader reader = resultStream.AsReader();

            for (int i = 0; i < batchCount; i++)
            {
                reader.BeginForEachIndex(i);
                int totalItems = reader.RemainingItemCount;

                if (totalItems > 1)
                {
                    // 데이터 구성: [Point 0, Point 1, ..., Point N, Count]
                    // 정점 데이터들만 담을 배열 (마지막 int 제외)
                    int pointCount = totalItems - 1;
                    float3[] path = new float3[pointCount];

                    for (int j = 0; j < pointCount; j++)
                    {
                        path[j] = reader.Read<float3>();
                    }

                    reader.Read<int>(); // 마지막 Count 값 읽어서 소모

                    // [옥토패스 트래블러용 팁] 
                    // Smoothing된 경로는 이미 직선이므로 별도의 Reverse 없이 그대로 반환
                    finalPaths.Add(path);
                }
                else
                {
                    finalPaths.Add(System.Array.Empty<float3>());
                }

                reader.EndForEachIndex();
            }

            nativeStarts.Dispose();
            nativeEnds.Dispose();
            resultStream.Dispose();

            return finalPaths;
        }
    }
}