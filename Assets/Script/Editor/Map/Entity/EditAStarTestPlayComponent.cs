using MessagePack;
using MessagePack.Resolvers;
using Script.Map.Data;
using Script.Map.Provider; // [추가됨] EditMapRepoProvider를 사용하기 위해 네임스페이스 추가
using Script.Map.Utility; // 첨부해주신 Utility의 네임스페이스
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

// [Framework] MonoBehaviour 상속 객체는 Component 명명 규칙 사용
public class EditAStarTestPlayComponent : MonoBehaviour
{
    [SerializeField] private Transform startTransform;
    [SerializeField] private Transform endTransform;

    // 런타임 데이터인 MapTileData를 저장하는 캐시
    private Dictionary<long, MapTileData> cachedTileDic;

    public async void Play()
    {
        Debug.Log($"Start Test Play: A* Pathfinding (Batch & Smoothed)");
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

                    // MapGridData로 정상 로드
                    MapGridData grid = MessagePackSerializer.Deserialize<MapGridData>(textAsset.bytes, options);

                    int gKey = grid.Key;
                    // NaviTileDict를 순회하여 캐시에 등록
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

        // [FIX] Batch 함수의 파라미터 규격(List)에 맞게 래핑하여 전달
        List<Vector3> startPositions = new List<Vector3> { startPos };
        List<Vector3> endPositions = new List<Vector3> { endPos };

        // [추가됨] 유틸리티에 전달하기 전, 에디터 전용 RepoProvider를 이용해 Dictionary를 NativeHashMap으로 변환/가져옵니다.
        // 이 스크립트 자체가 #if UNITY_EDITOR 안에 있으므로 에디터 클래스 호출이 전혀 문제되지 않습니다.
        var nativeMap = EditMapRepoProvider.GetOrCreateNativeMap(cachedTileDic);

        // [FIX] 반환 타입 일치 (여러 개의 경로(배열)를 담은 리스트 반환)
        // 변환된 nativeMap을 순수 함수인 AStarPathfinderUtil에 주입합니다.
        List<float3[]> batchPaths =
            AStarPathfinderUtil.RequestPathsBatch(startPositions, endPositions, nativeMap);

        stopwatch.Stop();
        Debug.Log($"Pathfind time: {stopwatch.ElapsedMilliseconds / 1000f:F3} seconds");

        // 요청한 1개의 경로 중 첫 번째 결과를 추출
        float3[] path = (batchPaths != null && batchPaths.Count > 0) ? batchPaths[0] : null;

        // [FIX] 추출된 경로는 배열(float3[])이므로 .Length 사용
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