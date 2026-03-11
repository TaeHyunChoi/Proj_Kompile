namespace Script.Map.Utility
{
    using System.Collections.Generic;
    using Script.Map.Data;
    using Script.Map.Provider;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    public static class AStarPathfinderUtil
    {
        public static List<float3[]> RequestPathsBatch(List<Vector3> starts, List<Vector3> ends,
            Dictionary<long, MapTileData> tileDic)
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

            var nativeMap = EditMapRepoProvider.GetOrCreateNativeMap(tileDic);

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