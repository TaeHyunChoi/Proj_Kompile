using Script.Map.Data;

#if UNITY_EDITOR
namespace Script.Map.Instance
{
using MessagePack;
using MessagePack.Resolvers;
using Script.Map;
using Script.Map.Utility;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class EditAStarTestPlayEntity : MonoBehaviour
{
    [SerializeField] private Transform startTransform;
    [SerializeField] private Transform endTransform;

    private Dictionary<long, EditMapTileData> cachedTileDic;

    public async void Play()
    {
        Debug.Log($"Start Test Play: A* Pathfinding");
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        if (null == cachedTileDic || 0 == cachedTileDic.Count)
        {
            cachedTileDic = new Dictionary<long, EditMapTileData>();

            string label = "MapNavi";
            var handle = Addressables.LoadAssetsAsync<TextAsset>(label, callback: (textAsset) =>
            {
                // 칵 파일이 로드될 때마다 실행되는 콜백 (병렬 실행)
                if (null != textAsset)
                {
                    var options =
                        MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
                    EditMapGridData grid = MessagePackSerializer.Deserialize<EditMapGridData>(textAsset.bytes, options);

                    int gKey = grid.gridKey;
                    foreach (var tKV in grid.Data)
                    {
                        int tKey = tKV.Key;
                        long id = MapCoordUtil.ComputeID(gKey, tKey);
                        cachedTileDic.Add(id, tKV.Value);
                    }
                }
            });
            await handle.Task;

            Addressables.Release(handle);
        }

        Vector3 startPos = startTransform.position;
        Vector3 endPos = endTransform.position;
        float3[] path = AStarPathfinder.RequestPathImmediate(startPos, endPos, cachedTileDic);

        stopwatch.ToString();
        Debug.Log($"pathfind time: {stopwatch.ElapsedMilliseconds / 1000f:F3} seconds");

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < path.Length; ++i)
        {
            sb.Append($"{path[i]} -> ");
        }

        sb.Append("[GOAL]");

        Debug.Log(sb.ToString());
    }
}    
}
#endif