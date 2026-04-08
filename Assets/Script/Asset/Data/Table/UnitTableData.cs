namespace Script.Global.Asset.Data
{
    using Script.Global.Unit.Data;
    using Script.Global.Unit.Entity;
    
    /// <summary> 기획 데이터 (CSV 등) 1줄에 해당하는 순수 데이터 정의. 메모리 복사 비용 및 GC 방지를 위해 struct로 정의
    /// </summary>
    [System.Serializable]
    public struct UnitTableData
    {
        public int UnitID;
        public string AssetAddress; //굳이 값형으로 남기고 싶다면 Unity.Collections.FixedBytes16 등을 사용
        public UnitType Type;
        public UnitBrainType BrainType;
        
        // 초기 스탯...
        // public int BaseMaxHP;
        // public float MoveSpeed;
        //...
    }
}