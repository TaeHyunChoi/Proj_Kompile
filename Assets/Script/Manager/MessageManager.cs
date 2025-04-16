namespace Script.Manager
{
    using Script.Interface;
    using Script.IngameMessage;
    using System.Collections.Generic;

    /// <summary>
    /// 비동기로 처리하기엔 사용하는 데이터가 (너무) 작아 동기적으로 처리
    /// UniTask와 다르게 한 번 받는다고 해제하지 않음 => 해제 시점을 직접 조작
    /// </summary>
    public static class MessageManager
    {
        private static readonly List<IMessageReceiver> ingameReceivers = new List<IMessageReceiver>();

        public static void AddReceiver(IMessageReceiver receiver, bool hasInput = false)
        {
            if (false == ingameReceivers.Contains(receiver))
            {
                for (int i = 0; i < ingameReceivers.Count; ++i)
                {
                    if (null == ingameReceivers[i])
                    {
                        ingameReceivers[i] = receiver;
                        return;
                    }
                }
                ingameReceivers.Add(receiver);
            }
        }

        public static void Publish<T>(IngameEventType type, T data) where T : struct
        {
            for (int i = ingameReceivers.Count - 1; i >= 0; --i)
            {
                if (null == ingameReceivers[i])
                {
                    continue;
                }

                ingameReceivers[i].Receive_IngameEvent(type, data);
            }
        }

        public static void Dispose(IMessageReceiver receiver)
        {
            for (int i = 0; i < ingameReceivers.Count; ++i)
            {
                if (receiver == ingameReceivers[i])
                {
                    ingameReceivers[i] = null;
                }
            }
        }
    }
}

