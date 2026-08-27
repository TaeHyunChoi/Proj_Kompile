namespace Kompile.Domain
{
    using System.Collections.Concurrent;
    using System.Threading;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    public static partial class AssetProvider // InstanceEntry
    {
        /// <summary> 프리팹 원본 핸들과 풀링된 인스턴스들을 관리하는 내부 클래스 </summary>
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

            /// <summary> [Zero-GC] 이미 풀 장부에 들어와서 대기 중인 오브젝트인지 검사 </summary>
            public bool IsAlreadyPooled(GameObject instance)
            {
                if (!UsePooling || _pool == null) return false;

                // LINQ 대신 순수 foreach를 사용하여 구조체 열거자를 통한 힙 할당 방지
                foreach (GameObject pooledItem in _pool)
                {
                    if (pooledItem == instance) return true;
                }

                return false;
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

            /// <summary> 세션 전면 해제 시 풀에 쌓인 가상 오브젝트들을 완전히 파괴하는 데드라인 함수 </summary>
            public void ClearAndDestroyPool()
            {
                if (!UsePooling || _pool == null) return;

                while (_pool.TryDequeue(out GameObject instance))
                {
                    if (instance != null)
                    {
                        // Addressables.InstantiateAsync로 만든 객체는 ReleaseInstance로 지워야 안전합니다.
                        Addressables.ReleaseInstance(instance);
                    }
                }
            }

            public void AddReference() => Interlocked.Increment(ref _referenceCount);
            public void RemoveReference() => Interlocked.Decrement(ref _referenceCount);

            // 참조 카운트가 0 이하이기만 하면 에셋 해제 조건 충족으로 변경하여 누수를 막습니다.
            public bool ShouldRelease() => _referenceCount <= 0;
        }
    }
}