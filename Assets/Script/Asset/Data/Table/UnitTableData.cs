namespace Kompile.Asset.Data
{
    using MessagePack;
    using Kompile.Unit.Data;
    using Unity.Collections;
    
    /// <summary> 기획 데이터 (CSV 등) 1줄에 해당하는 순수 데이터 정의. 메모리 복사 비용 및 GC 방지를 위해 struct로 정의 </summary>
    [System.Serializable]
    public struct UnitTableData
    {
        public int ID;
        public FixedString32Bytes AssetAddress;
        public FixedString32Bytes AocAddress;
        public UnitType Type;
        public UnitBrainType BrainType;

        [IgnoreMember]
        public string Address => AssetAddress.ToString();
        [IgnoreMember]
        public string AocAddressStr => AocAddress.ToString();
    }
}