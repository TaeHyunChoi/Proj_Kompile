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
        public static async Awaitable<float3[]> RequestPath(Vector3 startPos, Vector3 endPos, Dictionary<long, MapTileData> tileDic)
        {
            NativeHashMap<long, (long, long)> map = new NativeHashMap<long, (long, long)>(tileDic.Count, Allocator.TempJob);
            NativeList<float3> resultPath = new NativeList<float3>(Allocator.TempJob);
            List<float3> result = new List<float3>();

            try
            {
                foreach (var tile in tileDic)
                {
                    long naviMask = tile.Value.NaviMask;
                    long linkMask = tile.Value.LinkMask;
                    map.TryAdd(tile.Key, (naviMask, linkMask));
                }

                AStarPathJob job = new ()
                {
                    StartPos = startPos,
                    EndPos = endPos,
                    Radius = 0.3f,
                    Map = map,
                    ResultPath = resultPath
                };

                var handle = job.Schedule();
                while (false == handle.IsCompleted)
                {
                    await Awaitable.NextFrameAsync();
                }
                handle.Complete();


                for (int i = 0; i < resultPath.Length; ++i)
                {
                    result.Add(resultPath[i]);
                }
            }
            finally
            {
                map.Dispose();
                resultPath.Dispose();
            }

            return result.ToArray();
        }
    }
}