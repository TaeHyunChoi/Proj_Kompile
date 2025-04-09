using Script.Data;
using Script.Index;
using UnityEngine;

namespace Script.IngameMessage
{
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
}