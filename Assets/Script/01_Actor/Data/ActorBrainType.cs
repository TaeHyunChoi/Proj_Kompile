namespace Kompile.Data
{
    /// <summary> 필드 내 유닛의 상세 행동 패턴 (IUnitBrain 결정) </summary>
    public enum ActorBrainType
    {
        None = 0,
        Player,
        Party,
        NPC,    // unit.index 받아서 참조?
        Enemy,  // unit.index 받아서 참조?
    }
}