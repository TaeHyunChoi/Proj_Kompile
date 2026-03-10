using Script.Map.Data;

#if UNITY_EDITOR
namespace Script.Map.Entity
{
    using MessagePack;
    using MessagePack.Resolvers;
    using Script.Map.Utility;
    using System.Collections.Generic;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

// [Framework] MonoBehaviour 상속 객체는 Component 명칭 사용
    public class EditAStarTestPlayComponent : MonoBehaviour
    {
        [SerializeField] private Transform startTransform;
        [SerializeField] private Transform endTransform;

        // 런타임 데이터인 MapTileData를 저장하는 캐시
        private Dictionary<long, MapTileData> cachedTileDic;

        public async void Play()
        {
            Debug.Log($"Start Test Play: A* Pathfinding");
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            if (null == cachedTileDic || 0 == cachedTileDic.Count)
            {
                cachedTileDic = new Dictionary<long, MapTileData>();

                string label = "MapNavi";
                var handle = Addressables.LoadAssetsAsync<TextAsset>(label, callback: (textAsset) =>
                {
                    if (null != textAsset)
                    {
                        // ContractlessStandardResolver를 사용하되, 저장한 타입과 동일한 MapGridData로 역직렬화
                        var options =
                            MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);

                        // [FIX] EditMapGridData가 아닌 MapGridData로 로드해야 함
                        MapGridData grid = MessagePackSerializer.Deserialize<MapGridData>(textAsset.bytes, options);

                        int gKey = grid.Key;
                        // [FIX] NaviTileDict를 순회하여 캐시에 등록
                        foreach (var tKV in grid.NaviTileDict)
                        {
                            int tKey = tKV.Key;
                            MapCoordUtil.ComputeID(gKey, tKey, out long id);
                            cachedTileDic.Add(id, tKV.Value);
                        }
                    }
                });
                await handle.Task;

                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    Addressables.Release(handle);
                }
            }

            Vector3 startPos = startTransform.position;
            Vector3 endPos = endTransform.position;

            // 로드된 MapTileData 캐시를 기반으로 경로 탐색 수행
            float3[] path = AStarPathfinderUtil.RequestPathImmediate(startPos, endPos, cachedTileDic);

            stopwatch.Stop();
            Debug.Log($"Pathfind time: {stopwatch.ElapsedMilliseconds / 1000f:F3} seconds");

            if (path != null && path.Length > 0)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < path.Length; ++i)
                {
                    sb.Append($"{path[i]} -> ");
                }

                sb.Append("[GOAL]");
                Debug.Log(sb.ToString());
            }
            else
            {
                Debug.LogWarning("Path not found!");
            }
        }
    }
}
#endif