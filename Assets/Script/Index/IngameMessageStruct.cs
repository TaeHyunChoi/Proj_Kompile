namespace Script.IngameMessage
{
    using UnityEngine;
    using Script.Data;
    using Script.Index;

    public readonly struct OnEndProcess
    {
        public readonly AssetCode AssetCode;
        public readonly int endCode;

        public OnEndProcess(AssetCode index, int end = 0)
        {
            AssetCode = index;
            endCode = end;
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


    /// <summary> 로딩 커튼 on/off 완료 여부</summary>
    public readonly struct OnEndLoadingCurtain
    {
        public readonly bool isOn;
        public OnEndLoadingCurtain(bool on)
        {
            isOn = on;
        }
    }

    public readonly struct OnInput
    {
        public readonly IDxInput.InputFlag InputFlagValue;
        public OnInput(IDxInput.InputFlag flag)
        {
            InputFlagValue = flag;
        }
    }
}