namespace Script.Manager
{
    using Script.Index;
    using Script.Interface;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// 비동기로 처리하기엔 사용하는 데이터가 (너무) 작아 동기적으로 처리 <br/>
    /// UniTask와 다르게 한 번 받는다고 해제하지 않음 => 해제 시점을 직접 조작
    /// </summary>
    public static class MessageManager
    {
        private static readonly List<IMessageReceiver> ingameReceivers = new List<IMessageReceiver>();

        public static void AddReceiver(IMessageReceiver receiver)
        {
            if (false == ingameReceivers.Contains(receiver))
            {
                ingameReceivers.Add(receiver);
            }
            else
            {
#if UNITY_EDITOR
                Debug.Assert(false, $"Already Have Message Receiver ({receiver.ToString()})");
#endif
            }
        }

        public static void Publish<T>(T data) where T : struct
        {
            for (int i = ingameReceivers.Count - 1; i >= 0; --i)
            {
                if (null == ingameReceivers[i])
                {
                    continue;
                }

                ingameReceivers[i].ReceiveIngameMessage(data);
            }
        }

        public static void Dispose(IMessageReceiver receiverOrNull)
        {
            if (null == receiverOrNull)
            {
                return;
            }

            for (int i = 0; i < ingameReceivers.Count; ++i)
            {
                if (receiverOrNull == ingameReceivers[i])
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.Log($"{ingameReceivers[i].GetType().Name}.Dispose()");
#endif
                    ingameReceivers.RemoveAt(i);
                }
            }
        }
    }
}