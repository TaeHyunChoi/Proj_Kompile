namespace Script.Util
{
    using System.Threading;

    public class ReaderWriterLock
    {
        private int _readers = 0; // 현재 읽는 중인 스레드 수
        private int _writers = 0; // 쓰는 중인 스레드 수
        private readonly object _lock = new object();

        public void EnterReadLock()
        {
            lock (_lock)
            {
                while (_writers > 0) // 쓰기 중이면 대기
                {
                    Monitor.Wait(_lock);
                }
                _readers++;
            }
        }

        public void ExitReadLock()
        {
            lock (_lock)
            {
                _readers--;
                if (_readers == 0)
                {
                    Monitor.PulseAll(_lock); // 대기 중인 쓰기 스레드 깨우기
                }
            }
        }

        public void EnterWriteLock()
        {
            lock (_lock)
            {
                while (_writers > 0 || _readers > 0) // 읽기/쓰기 중이면 대기
                {
                    Monitor.Wait(_lock);
                }
                _writers++;
            }
        }

        public void ExitWriteLock()
        {
            lock (_lock)
            {
                _writers--;
                Monitor.PulseAll(_lock); // 대기 중인 읽기/쓰기 스레드 깨우기
            }
        }
    }

}