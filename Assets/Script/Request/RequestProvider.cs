using UnityEngine;
using Kompile.Data;
using System.Collections.Generic;

namespace Kompile.Provider
{
    public static class RequestProvider<T> where T: RequestBase, new()
    {
        private static readonly List<T> _pool = new List<T>(64);
        
        public static T Get()
        {
            if (_pool.Count > 0)
            {
                int lastIndex = _pool.Count - 1;
                T req = _pool[lastIndex];
                req.IsPooled = false;
                _pool.RemoveAt(lastIndex);

                return req;
            }

            return new T();
        }
        public static void Return(T request)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (request == null) 
            {
                Debug.LogError("Null 반납 차단"); 
                return; 
            }
            if (request.IsPooled)
            {
                Debug.LogError("중복 반납 차단"); 
                return;
            }
            if (request.GetType() != typeof(T))
            {
                Debug.LogError("타입 불일치 차단"); 
                return;
            }
#endif
            request.Clear();
            request.IsPooled = true;

            _pool.Add(request);
        }
    }

    public static class RequestProviderExtensions
    {
        public static void ReturnToPool<T>(this T request) where T : RequestBase, new()
        {
            RequestProvider<T>.Return(request);
        }
    }
}
