namespace Script.Field.Data
{
    /// <summary> 필드 내 유닛의 논리적 구분을 위한 열거형 (Data 계층의 Type 정의) </summary>
    public enum UnitType
    {
        Player,
        PartyGroup,
        NPC,
        Enemy
    }
    /// <summary> 필드 내 유닛의 상세 행동 패턴을 정의합니다. </summary>
    public enum FieldBrainType
    {
        PlayerControl,        // 유저 조작
        PartyFollower,        // 플레이어 뒤를 졸졸 따라다니는 파티원
        NpcIdle,              // 제자리에서 대기하는 일반 NPC
        NpcShop,              // 상점 UI를 호출하는 NPC
        NpcInn,               // 여관 UI 및 회복 이벤트를 발생시키는 NPC
        EnemyWanderEncounter, // 주변을 배회하다가 플레이어가 닿으면 전투 진입
        EnemyStandEncounter   // 길목을 막고 서 있다가 플레이어가 닿으면 전투 진입
    }
    
    public class UnitRuntimeContext
    {
        public UnitType Type { get; private set; }
        public FieldBrainType BrainType { get; private set; } // 상세 분류 추가

        public UnitRuntimeContext(UnitType type, FieldBrainType brainType)
        {
            Type = type;
            BrainType = brainType;
        }
    }
}