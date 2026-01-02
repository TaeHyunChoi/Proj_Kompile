namespace Script.Map
{
    using Script.Data;
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    public class AStarPathfinder
    {
        public static float3[] RequestPathImmediate(Vector3 startPos, Vector3 endPos, Dictionary<long, MapTileData> tileDic)
        {
            // 1. 캐시된 맵 데이터 가져오기 (여기서 시간 단축!)
            var nativeMap = EditMapCache.GetOrCreateNativeMap(tileDic);

            // 2. 결과 담을 리스트
            NativeList<float3> resultPath = new NativeList<float3>(Allocator.TempJob);
            List<float3> result = new List<float3>();

            try
            {
                // 3. Job 생성
                AStarPathJob job = new AStarPathJob
                {
                    StartPos = startPos,
                    EndPos = endPos,
                    // [MapSampling] 규칙: 이동체는 반경 0.45f 내의 서브 타일을 체크해야 함
                    Radius = 0.325f,
                    Map = nativeMap,
                    ResultPath = resultPath
                };

                // 4. 즉시 실행 및 대기 (Run은 메인 스레드에서 즉시 실행, Schedule().Complete()와 유사하나 오버헤드가 적음)
                job.Run();

                // 5. 결과 변환
                foreach (var p in resultPath)
                {
                    result.Add(p);
                }
            }
            finally
            {
                // 결과 리스트는 꼭 해제 (맵은 해제하지 않음)
                if (true == resultPath.IsCreated)
                {
                    resultPath.Dispose();
                }
            }

            return result.ToArray();
        }
    }
}