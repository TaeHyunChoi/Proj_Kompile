namespace Script.Manager
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 비동기로 처리하기엔 사용하는 데이터가 (너무) 작아 동기적으로 처리
    /// UniTask와 다르게 한 번 받는다고 해제하지 않음 => 해제 시점을 직접 조작
    /// </summary>
    public static class MessageManager
    {
        private static readonly List<IMessageReceiver> receivers = new List<IMessageReceiver>();

        public static void AddReceiver(IMessageReceiver targetReceiver)
        {
            if (false == receivers.Contains(targetReceiver))
            {
                receivers.Add(targetReceiver);
            }
        }
        public static void Publish(Message_t msg)
        {
            for (int i = 0; i < receivers.Count; ++i)
            {
                receivers[i].Receive(msg);
            }
        }
        public static void Dispose(IMessageReceiver receiver)
        {
            receivers.Remove(receiver);
        }
    }

    public interface IMessageReceiver
    {
        public void Receive(Message_t msg);
    }
    public readonly struct Message_t
    {
        private readonly MessageType type;
        private readonly int index;
        private readonly Object value;

        public Message_t(MessageType targetType, int targetIndex, Object targetValue)
        {
            type  = targetType;
            index = targetIndex;
            value = targetValue;
        }

        public readonly MessageType GetMessageType()
        {
            return type;
        }
        public readonly int GetIndex()
        {
            return index;
        }
        public readonly T GetValue<T>() where T : class 
        {
            return value as T;
        }
    }
    public enum MessageType
    { 
        NONE,

        GET_ASSET, 
        END_OBJECT_PROCESS,
    }
}

