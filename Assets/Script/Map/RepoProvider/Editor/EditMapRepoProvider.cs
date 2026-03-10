#if UNITY_EDITOR
namespace Script.Map.Provider
{
    using System.Collections.Generic;
    using Unity.Collections;
    using UnityEditor;
    using UnityEngine;
    using Script.Map.Data;
    
    /// <summary>
    /// [Framework] System 분류: [MapSampling]에 필요한 네이티브 캐시 자원을 관리하고 제공함
    /// </summary>
    [InitializeOnLoad]
    public static class EditMapRepoProvider
    {
        private static readonly EditMapCacheContextData _cacheData = new EditMapCacheContextData();

        static EditMapRepoProvider()
        {
            // 도메인 리로드 및 에디터 종료 시 메모리 누수 방지
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            EditorApplication.quitting += Clear;
        }

        /// <summary>
        /// Manager가 요청할 때 캐시된 데이터를 제공하거나 새로 생성하여 반환함
        /// </summary>
        public static NativeHashMap<long, (long, long)> GetOrCreateNativeMap(Dictionary<long, MapTileData> tileDic, bool forceToCreate = false)
        {
            // 규칙: 데이터 개수가 동일하면 기존 System 자원을 재사용
            if (_cacheData.IsValid && _cacheData.LastCount == tileDic.Count)
            {
                if (!forceToCreate) return _cacheData.NativeMap;
            }

            Clear();

            // [MapSampling] NaviMask(4-bit interval) 및 LinkMask 정보를 네이티브 메모리에 배치
            var nativeMap = new NativeHashMap<long, (long, long)>(tileDic.Count, Allocator.Persistent);
            foreach (var kv in tileDic)
            {
                nativeMap.TryAdd(kv.Key, (kv.Value.NaviMask, kv.Value.LinkMask));
            }

            _cacheData.NativeMap = nativeMap;
            _cacheData.LastCount = tileDic.Count;

            Debug.Log($"[MapSampling] {tileDic.Count}개 타일 데이터 캐싱 완료 (System 자원)");
            return _cacheData.NativeMap;
        }

        public static void Clear()
        {
            _cacheData.Dispose();
        }
    }
}
#endif