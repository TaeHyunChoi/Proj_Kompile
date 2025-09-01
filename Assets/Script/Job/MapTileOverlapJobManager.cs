namespace Script.Manager
{
    using Script.Data;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using System.Diagnostics;

    public class MapTileOverlapJobManager
    {
        private static MapTileOverlapJobManager instance;
        public static MapTileOverlapJobManager  Instance => instance;

        private NativeArray<IngameMapTileData>  ingameMapTileDatas;
        private NativeArray<bool>               results;

        private JobHandle   jobHandle;
        private bool        isJobScheduled;

        public MapTileOverlapJobManager()
        {
            instance = this;

            jobHandle = new JobHandle();
            isJobScheduled = false;

            int target_count = 4;
            ingameMapTileDatas = new NativeArray<IngameMapTileData>(target_count, Allocator.Persistent);
            results = new NativeArray<bool>(target_count, Allocator.Persistent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        /// <param name="tile">언제나 4개씩 들어온다. 하나라도 없으면 함수 호출을 하지 않는다.</param>
        public void ScheduleJob_MapTileMovable(Vector3 position, bool isSmall, float radius, params IngameMapTileData[] tiles)
        {
            if (true == isJobScheduled)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Log($"[MapTileOverlapManager] Job is already scheduled. Please wait for the current job to complete.");
#endif
                return;
            }

            int length = tiles.Length;
            for (int i = 0; i < length; ++i)
            {
                ingameMapTileDatas[i] = tiles[i];
                results[i] = false;
            }

            MapTileMovableJob job = new MapTileMovableJob
            {
                IngameMapTileData   = ingameMapTileDatas,
                SphereCenter        = new float3(position.x, position.y, position.z),
                SphereRadius        = radius,
                Results             = results
            };


            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < job.IngameMapTileData.Length; ++i)
            {
                job.Execute(i);
            }

            // 6. Job 실행 시간 측정 종료
            stopwatch.Stop();

            // 7. 결과 출력
            UnityEngine.Debug.Log($"Job Execution Time: {stopwatch.Elapsed.TotalMilliseconds:F2} ms");

        }

        ~MapTileOverlapJobManager()
        {
            // DisposeNativeArrays()
            if (ingameMapTileDatas.IsCreated)
            { 
                ingameMapTileDatas.Dispose();
            }
            if (results.IsCreated)
            {
                results.Dispose();
            }
        }
    }
}
