namespace Script.Global.Asset.Data
{
    using MessagePack; // IgnoreMember 사용을 위해 추가
    using Script.Global.Unit.Data;
    using Unity.Collections;
    
    /// <summary> 기획 데이터 (CSV 등) 1줄에 해당하는 순수 데이터 정의. 메모리 복사 비용 및 GC 방지를 위해 struct로 정의
    /// </summary>
    [System.Serializable]
    public struct UnitTableData
    {
        public int ID;
        public FixedString32Bytes AssetAddress; 
        public UnitType Type;
        public UnitBrainType BrainType;

        // 직렬화에서 제외 (순수 연산용 프로퍼티이므로 에러 방지)
        [IgnoreMember]
        public string Address => AssetAddress.ToString();
    }
}