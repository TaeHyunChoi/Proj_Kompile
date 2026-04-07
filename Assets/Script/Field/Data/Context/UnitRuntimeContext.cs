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

    /// <summary> Entity의 현재 상태 정보만을 담는 순수 데이터 클래스 </summary>
    public class UnitRuntimeContext
    {
        // --- Identity / Classification ---
        // 생성 시점에 부여되어 Manager의 분류 기준이 되는 타입 정보
        public UnitType Type { get; private set; }

        // --- Status ---
        public int MaxHp = 100;
        public int CurrentHp = 100;
        public bool IsDead = false;

        /// <summary>
        /// Manager에서 인스턴스를 발급할 때 논리적 타입을 지정하여 생성합니다.
        /// </summary>
        public UnitRuntimeContext(UnitType type)
        {
            Type = type;
        }
    }
}