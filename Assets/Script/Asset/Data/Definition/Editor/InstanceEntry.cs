namespace Script.Asset.Provider
{
    using UnityEngine;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using System.Collections.Concurrent;
    using System.Threading;

    public static partial class AssetProvider // InstanceEntry
    {
        /// <summary>
        /// 프리팹 원본 핸들과 풀링된 인스턴스들을 관리하는 내부 클래스입니다.
        /// </summary>
        private class InstanceEntry
        {
            public AsyncOperationHandle Handle { get; }
            public bool UsePooling { get; }

            private int _referenceCount;
            private readonly ConcurrentQueue<GameObject> _pool;

            public InstanceEntry(AsyncOperationHandle handle, bool usePooling)
            {
                Handle = handle;
                UsePooling = usePooling;
                _referenceCount = 0;

                if (usePooling)
                {
                    _pool = new ConcurrentQueue<GameObject>();
                }
            }

            public bool TryGetPooledInstance(out GameObject instance)
            {
                instance = null;
                if (!UsePooling) return false;

                // 이미 파괴된 객체가 풀에 남아있을 수 있으므로 유효성 검증
                while (_pool.TryDequeue(out instance))
                {
                    if (instance != null) return true;
                }

                return false;
            }

            public void ReturnToPool(GameObject instance)
            {
                if (UsePooling && instance != null)
                {
                    _pool.Enqueue(instance);
                }
            }

            public void AddReference() => Interlocked.Increment(ref _referenceCount);
            public void RemoveReference() => Interlocked.Decrement(ref _referenceCount);

            public bool ShouldRelease() => _referenceCount <= 0 && (!UsePooling || _pool.IsEmpty);
        }
    }
}