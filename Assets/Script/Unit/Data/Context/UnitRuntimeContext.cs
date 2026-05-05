namespace Kompile.Unit.Data
{
    /// <summary> 유닛의 기본 설정값을 저장 </summary>
    public struct UnitRuntimeContext
    {
        public UnitType Type { get; private set; }
        public UnitBrainType BrainType { get; private set; }
        
        // public int MaxHP;
        // public int CurrentHP;
        // ...

        public UnitRuntimeContext(UnitType type, UnitBrainType brainType)
        {
            Type = type;
            BrainType =  brainType;
            
            // 데이터 테이블, 레벨 수치 등을 계산하여 스탯 산정
        }
    }
}