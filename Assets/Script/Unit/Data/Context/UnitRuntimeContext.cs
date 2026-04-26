namespace Kompile.Unit.Data
{
    public struct UnitRuntimeContext
    {
        public UnitType Type { get; private set; }
        public UnitBrainType BrainType { get; private set; }
        
        // 스탯
        // public int MaxHP;
        // public int CurrentHP;

        public UnitRuntimeContext(UnitType type, UnitBrainType brainType)
        {
            Type = type;
            BrainType =  brainType;
        }
    }
}