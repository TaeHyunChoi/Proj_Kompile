namespace Script.Manager
{
    using Script.Data;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    public class MapTileOverlapJobManager
    {
        private const int TRIANGLES_COUNT = 16;

        private static MapTileOverlapJobManager instance;
        public static MapTileOverlapJobManager Instance => instance;

        private NativeArray<float3> triangleA;
        private NativeArray<float3> triangleB;
        private NativeArray<float3> triangleC;
        private NativeArray<bool>   overlapResults;

        private JobHandle jobHandle;
        private bool isJobScheduled = false;

        public MapTileOverlapJobManager()
        {
            instance = this;

            jobHandle = new JobHandle();

            int total_count = 16 * 4;
            triangleA      = new NativeArray<float3>(total_count, Allocator.Persistent);
            triangleB      = new NativeArray<float3>(total_count, Allocator.Persistent);
            triangleC      = new NativeArray<float3>(total_count, Allocator.Persistent);
            overlapResults = new NativeArray<bool>  (total_count, Allocator.Persistent);
        }


        public bool TryGetOverlapResults(out bool[] result)
        {
            result = null;

            if (false == isJobScheduled)
            {
                //// 2. 현재 위치를 업데이트합니다.
                ////lastPosition = transform.position;

                //// 3. 주변 4개의 타일 pivot 데이터를 가져옵니다.
                //// 이 예시에서는 미리 nearbyTiles 배열에 데이터가 채워져 있다고 가정합니다.
                //// 실제 구현에서는 이 시점에서 타일맵 데이터베이스에서 주변 타일을 찾아와야 합니다.

                //// 4. Job을 스케줄링합니다.
                //MapTileOverlapJobManager.Instance.ScheduleOverlapCheck(
                //     playerPosition,
                //     playerRadius,
                //     nearbyTiles[0], nearbyTiles[1], nearbyTiles[2], nearbyTiles[3]
                // );
            }

            //// 5. Job이 완료되었는지 확인하고 결과를 처리합니다.
            //if (MapTileOverlapJobManager.Instance.CheckIfJobIsDone(out bool[] results))
            //{
            //    // results는 4개의 bool 값을 가진 배열입니다.
            //    // results[0]는 nearbyTiles[0]의 결과, results[1]은 nearbyTiles[1]의 결과...
            //    Debug.Log("Overlap check completed. Results are available.");
            //    for (int i = 0; i < results.Length; i++)
            //    {
            //        if (results[i])
            //        {
            //            Debug.Log($"Tile {i} overlaps with the player.");
            //        }
            //    }
            //}

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="tile">언제나 4개씩 들어온다. 하나라도 없으면 함수 호출을 하지 않는다.</param>
        public void ScheduleCheckOverlapTrianglesInTile(Vector3 next_tile_pivot, Vector3 center, float radius, params IngameMapTileData[] tiles)
        {
            if (true == isJobScheduled)
            {
#if UNITY_EDITOR
                Debug.Log($"[MapTileOverlapManager] Job is already scheduled. Please wait for the current job to complete.");
#endif
                return;
            }

            // 쓰읍.. 여기 뭔가 마음에 안드는데..
            IngameMapTileData tile;
            float3 pivot;
            int index = 0;
            for (int i = 0; i < tiles.Length; ++i)
            {
                tile = tiles[i];
                pivot = new float3(next_tile_pivot.x, next_tile_pivot.y, next_tile_pivot.z);

                for (int j = 0; j < TRIANGLES_COUNT; ++j)
                {
                    triangleA[index] = pivot + new float3(tile.GetTrianglePoints(j, 0));
                    triangleB[index] = pivot + new float3(tile.GetTrianglePoints(j, 1));
                    triangleC[index] = pivot + new float3(tile.GetTrianglePoints(j, 2));
                    ++index;
                }
            }

            // 5. 단일 Job 인스턴스를 생성하고 필요한 데이터를 할당합니다.
            // CircleCenter와 CircleRadius는 단일 값으로 전달됩니다.
            TriangleCircleOverlapJob job = new TriangleCircleOverlapJob
            {
                TriangleA =  triangleA,
                TriangleB =  triangleB,
                TriangleC =  triangleC,

                SphereCenter = new float3(center.x, center.y, center.z),
                SphereRadius = radius,

                OverlapResults = overlapResults
            };

            jobHandle = job.Schedule(arrayLength: TRIANGLES_COUNT * 4, innerloopBatchCount: 64);
            isJobScheduled = true;
        }

        public bool CheckIfJobIsDone()
        {
            if (false == isJobScheduled)
            {
                return false;
            }

            bool isDone = true;

            if (true == jobHandle.IsCompleted)
            {
                jobHandle.Complete();
                isJobScheduled = false;

                for (int i = 0; i < overlapResults.Length; ++i)
                {
                    // 현재 내 코드로는 '단순히 overlap되지 않았다'로 판단할 수가 없다.
                    // overlap이 되지 않더라도 상관 없는 삼각형이 존재한다.
                    // overlap이 되었는데 그 부분에 데이터가 없다!가 원하는 바이다.
                    if (false == overlapResults[i])
                    {
                        isDone = false;
                        break;
                    }
                }
            }
            else
            {
                isDone = false;
            }

            return isDone;
        }

        ~MapTileOverlapJobManager()
        {
            // DisposeNativeArrays()
            if (triangleA.IsCreated)      { triangleA.Dispose(); }
            if (triangleB.IsCreated)      { triangleB.Dispose(); }
            if (triangleC.IsCreated)      { triangleC.Dispose(); }
            if (overlapResults.IsCreated) { overlapResults.Dispose(); }
        }
    }
}
