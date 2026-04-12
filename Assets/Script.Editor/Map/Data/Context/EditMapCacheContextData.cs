#if UNITY_EDITOR
namespace Kompile.Map.Editor.Data
{
    using Unity.Collections;
    
    /// <summary>
    /// [Framework] Data 분류: 맵 샘플링 연산에 사용되는 네이티브 캐시 데이터
    /// </summary>
    public class EditMapCacheContextData
    {
        public NativeHashMap<long, (long navi, long link)> NativeMap;
        public int LastCount = -1;

        public bool IsValid => NativeMap.IsCreated;

        public void Dispose()
        {
            if (true == NativeMap.IsCreated)
            {
                NativeMap.Dispose();
            }
            LastCount = -1;
        }
    }
}
#endif