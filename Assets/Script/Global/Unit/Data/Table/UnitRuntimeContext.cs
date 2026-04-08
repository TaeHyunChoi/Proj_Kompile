namespace Script.Global.Unit.Data
{
    using Script.Global.Asset.Data;
    
    public struct UnitRuntimeContext
    {
        public UnitType Type { get; private set; }
        public UnitBrainType BrainType { get; private set; }

        public bool IsDead;
        
        // 스탯
        // public int MaxHP;
        // public int CurrentHP;

        public UnitRuntimeContext(UnitType type, UnitBrainType brainType)
        {
            Type = type;
            BrainType =  brainType;
            IsDead = false;
        }
    }
}