namespace Script.Manager
{
    using Script.Data;
    using Script.Index;
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
        public static void Publish<T>(MessageType type, T data) where T : struct
        {
            for (int i = 0; i < receivers.Count; ++i)
            {
                receivers[i].Receive(type, data);
            }
        }
        public static void Dispose(IMessageReceiver receiver)
        {
            receivers.Remove(receiver);
        }
    }

    public interface IMessageReceiver
    {
        public void Receive<T>(MessageType type, T data) where T : struct;
    }

    // 얘도 다른 스크립트 파일로 넘기고
    public enum MessageType
    { 
        NONE,

        GET_ASSET, 
        END_OBJECT_PROCESS,
    }


    // 얘도 분류해야겠네..
    public readonly struct OnEndProcess
    {
        public readonly AssetCode AssetCode;

        public OnEndProcess(AssetCode index)
        {
            AssetCode = index;
        }
    }
    public readonly struct OnGetAsset_GameObject
    {
        public readonly AssetCode AssetCode;
        public readonly GameObject GameObject;
        public OnGetAsset_GameObject(AssetCode index, GameObject targetObj)
        {
            AssetCode = index;
            GameObject = targetObj;
        }
    }
    public readonly struct OnGetAsset_MapGridData
    {
        public readonly AssetCode AssetCode;
        public readonly MapGridData Data;
        public OnGetAsset_MapGridData(AssetCode index, MapGridData data)
        {
            AssetCode = index;
            Data = data;
        }
    }
}

