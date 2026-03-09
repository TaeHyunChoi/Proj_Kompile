namespace Script.Asset.Provider
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.ResourceManagement.AsyncOperations;

    public static partial class AssetProvider // InstanceEntry
    {
        /// <summary> Provider 계층의 데이터/에셋 공급 단위.
        /// 메모리 단편화를 줄이고 효율을 높이기 위해 sealed 예약어와 Stack 구조를 사용합니다.
        /// </summary>
        private sealed class InstanceEntry
        {
            public AsyncOperationHandle<GameObject> Handle { get; }

            // Queue -> Stack으로 변경 (LIFO). 
            // 가장 최근에 비활성화된 객체를 다시 꺼내 쓰므로 CPU 캐시 적중률(Cache Locality)이 더 높습니다.
            public Stack<GameObject> Pool { get; }

            public bool UsePooling { get; }
            public int ReferenceCount { get; private set; }

            public InstanceEntry(AsyncOperationHandle<GameObject> handle, bool usePooling)
            {
                Handle = handle;
                UsePooling = usePooling;
                // 초기 용량을 지정해주면 불필요한 배열 확장을 막아 메모리에 더 효율적입니다.
                Pool = new Stack<GameObject>(16);
                ReferenceCount = 0;
            }

            /// <summary>
            /// 풀에서 사용 가능한 인스턴스가 있는지 확인하고, 있다면 반환합니다.
            /// (기존 HasPooledInstance를 더 안전하고 직관적인 TryGet 패턴으로 개선)
            /// </summary>
            public bool TryGetPooledInstance(out GameObject instance)
            {
                instance = null;
                while (Pool.Count > 0)
                {
                    instance = Pool.Pop(); // Stack이므로 Pop 사용

                    // 씬 이동 등으로 인해 강제 파괴된(Missing) 객체가 아니라면 유효함
                    if (instance != null)
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// 인스턴스를 풀에 반환합니다.
            /// </summary>
            public void ReturnToPool(GameObject instance)
            {
                if (instance != null)
                {
                    Pool.Push(instance);
                }
            }

            public void AddReference() => ReferenceCount++;

            // 참조 카운트는 음수가 될 수 없으므로 안전하게 방어
            public void RemoveReference() => ReferenceCount = Mathf.Max(0, ReferenceCount - 1);

            // 사용 중인 객체도 없고, 풀에 대기 중인 객체도 없다면 메모리 해제 대상
            public bool ShouldRelease() => ReferenceCount <= 0 && Pool.Count == 0;
        }
    }
}