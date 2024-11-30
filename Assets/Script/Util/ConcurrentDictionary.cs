namespace Script.Util
{
    using System;
    using System.Collections.Generic;
    
    public class ConcurrentDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();
        private readonly ReaderWriterLock _lock = new ReaderWriterLock();

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            _lock.EnterReadLock();
            try
            {
                if (_dictionary.TryGetValue(key, out var value))
                    return value;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            _lock.EnterWriteLock();
            try
            {
                // 다시 확인 (다른 스레드가 추가했을 수 있음)
                if (!_dictionary.TryGetValue(key, out var value))
                {
                    value = valueFactory(key);
                    _dictionary[key] = value;
                }

                return value;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool TryAdd(TKey key, TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                return _dictionary.TryAdd(key, value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                return _dictionary.Remove(key, out value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            _lock.EnterReadLock();
            try
            {
                return _dictionary.TryGetValue(key, out value);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _dictionary.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}