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

        public static void Publish<T>(MessageType type, T data) where T : struct
        {
            for (int i = ingameReceivers.Count - 1; i >= 0; --i)
            {
                ingameReceivers[i]?.Receive(type, data);
                //if (null != ingameReceivers[i])
                //{
                //    ingameReceivers[i].Receive(type, data);
                //}
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
                if (true == inputReceivers[i].Receive(MessageType.INPUT_CONTROL, onInput))
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




    public interface IMessageReceiver
    {
        public bool Receive<T>(MessageType type, T data) where T : struct;
    }





    // 얘도 다른 스크립트 파일로 넘기고 - 파일을 어찌 넘겨야 좋으려나?
    public enum MessageType
    { 
        NONE,

        INPUT_CONTROL,

        GET_ASSET, 
        END_OBJECT_PROCESS,
        SELECT_ITEM,
    }
    public readonly struct OnInputControl
    {
        public readonly IDxInput.InputFlag inputFlag;

        public OnInputControl(IDxInput.InputFlag inputFlagValue)
        {
            inputFlag = inputFlagValue;
        }
    }
    public readonly struct OnEndProcess
    {
        public readonly AssetCode AssetCode;
        public readonly int endCode;

        public OnEndProcess(AssetCode index, int end = 0)
        {
            AssetCode = index;
            endCode   = end;
        }
    }
    public readonly struct OnGetAsset_GameObject
    {
        public readonly AssetCode AssetCode;
        public readonly GameObject GameObject;
        public int InstanceID => GameObject.GetInstanceID();
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
    public readonly struct OnSelect_UITitleMenu
    {
        public readonly int ValueInt;
        public OnSelect_UITitleMenu(int value)
        {
            ValueInt = value;
        }
    }
}

