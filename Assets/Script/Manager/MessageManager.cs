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
        private static readonly List<IMessageReceiver> inputReceivers  = new List<IMessageReceiver>();

        public static void AddReceiver(IMessageReceiver receiver, bool hasInput = false)
        {
            if (false == ingameReceivers.Contains(receiver))
            {
                for (int i = 0; i < ingameReceivers.Count; ++i)
                {
                    if (null == ingameReceivers[i])
                    {
                        ingameReceivers[i] = receiver;
                        goto ADD_INPUT_RECEIVER;
                    }
                }
                ingameReceivers.Add(receiver);
            }

        ADD_INPUT_RECEIVER:
            if (false == hasInput)
            {
                return;
            }
            else if (false == inputReceivers.Contains(receiver))
            {
                for (int i = 0; i < inputReceivers.Count; ++i)
                {
                    if (null == inputReceivers[i])
                    {
                        inputReceivers[i] = receiver;
                        return;
                    }
                }
                inputReceivers.Add(receiver);
            }
        }

        public static void Publish<T>(IngameMessageType type, T data) where T : struct
        {
            for (int i = ingameReceivers.Count - 1; i >= 0; --i)
            {
                if (null == ingameReceivers[i])
                {
                    continue;
                }

                ingameReceivers[i].Receive(type, data);
            }
        }
        public static void PublishInput(OnInputControl onInput)
        {
            for (int i = inputReceivers.Count - 1; i >= 0; --i)
            {
                if (null == inputReceivers[i])
                {
                    continue;
                }
                if (true == inputReceivers[i].Receive(IngameMessageType.INPUT_CONTROL, onInput))
                {
                    return;
                }
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

            for (int i = 0; i < inputReceivers.Count; ++i)
            {
                if (receiver == inputReceivers[i])
                {
                    inputReceivers[i] = null;
                }
            }
            //ingameReceivers.Remove(receiver);
            //inputReceivers.Remove(receiver);
        }
    }
}

