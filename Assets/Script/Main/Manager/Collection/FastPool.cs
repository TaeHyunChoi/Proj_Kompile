namespace Kompile.Main.Manager
{
    using UnityEngine;

    /// <summary> C++ 스타일의 인덱스 포인터 방식을 적용한 범용, Zero-GC 오브젝트 풀 </summary>
    public class FastPool<T> where T : Component
    {
        private T[] _pool;
        private int _count;

        public FastPool(int capacity = 32)
        {
            _pool = new T[capacity];
            _count = 0;
        }

        public void Push(T entity)
        {
            // 배열이 꽉 찼을 경우에만 2배로 확장 (메모리 재할당 최소화)
            if (_count >= _pool.Length)
            {
                T[] newPool = new T[_pool.Length * 2];
                System.Array.Copy(_pool, newPool, _pool.Length);
                _pool = newPool;
            }

            _pool[_count++] = entity;
        }

        public T Pop()
        {
            if (_count > 0)
            {
                // 값은 지우지 않고 인덱스만 감소시켜 반환 (O(1), No GC)
                return _pool[--_count];
            }
            return null;
        }

        public bool HasAvailable() => _count > 0;

        /// <summary>
        /// 씬 언로드 등 풀을 완전히 비워야 할 때 사용합니다.
        /// 제약조건(where T : Component) 덕분에 gameObject에 안전하게 접근합니다.
        /// </summary>
        public void ClearAndDestroyAll()
        {
            for (int i = 0; i < _count; i++)
            {
                if (_pool[i] != null)
                {
                    Object.Destroy(_pool[i].gameObject);
                }
            }
            _count = 0;
            _pool = null; // GC에 배열 메모리 위임
        }
    }
}