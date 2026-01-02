#if UNITY_EDITOR
namespace Script.Map
{
    using System.Collections.Generic;
    using Unity.Collections;
    using UnityEditor;
    using UnityEngine;
    using Script.Data;

    [InitializeOnLoad] // 스크립트가 다시 컴파일되거나 에디터가 로드될 때에 메모리를 자동으로 정리한다.
    public static class EditMapCache
    {
        private static NativeHashMap<long, (long, long)> nativeMap;
        private static int lastCount = -1;

        static EditMapCache() // 정적 생성자에는 접근 제한자(private...)를 사용할 수 없다. (정적 생성자는 '누가 호출할지' 정할 수 없으므로)
        {
            // 도메인 리로드(스크립트 수정 등) 시 메모리 누수 방지를 위하여 자동 해제를 등록
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            EditorApplication.quitting += Clear;
        }

        public static NativeHashMap<long, (long, long)> GetOrCreateNativeMap(Dictionary<long, MapTileData> tileDic, bool forceToCreate = false)
        {
            // case: 이미 캐시가 있고, 데이터 개수가 동일하다면 재사용
            if (true == nativeMap.IsCreated && lastCount == tileDic.Count)
            {
                // (재)생성이 강제라면 이전 데이터를 반환하지 않는다.
                if (false == forceToCreate)
                {
                    return nativeMap;
                }
            }

            Clear();

            nativeMap = new NativeHashMap<long, (long, long)>(tileDic.Count, Allocator.Persistent);
            foreach (var kv in tileDic)
            {
                var id = kv.Key;
                var tile = kv.Value;

                nativeMap.TryAdd(id, (tile.NaviMask, tile.LinkMask));
            }

            Debug.Log($"[MapSampling] 맵 데이터 캐싱 완료: {tileDic.Count}개 타일 (Allocator.Persistent)");

            lastCount = tileDic.Count;
            return nativeMap;
        }

        public static void Clear()
        {
            if (true == nativeMap.IsCreated)
            {
                nativeMap.Dispose();
                lastCount = -1;
            }
        }
    }
}
#endif