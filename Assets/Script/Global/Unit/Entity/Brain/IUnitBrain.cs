namespace Script.Unit.Entity
{
    /// <summary> 유닛의 행동 패턴을 정의하는 순수 C# 전략 개체</summary>
    public interface IUnitBrain
    {
        void Initialize(UnitEntityBase ownerEntity);
        void ManualUpdate();
        void Clear();
    }
}