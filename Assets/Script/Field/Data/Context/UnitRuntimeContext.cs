namespace Script.Field.Data
{
    /// <summary> Entity의 현재 상태 정보만을 담는 순수 데이터 클래스 </summary>
    public class UnitRuntimeContext
    {
        public int MaxHp = 100;
        public int CurrentHp = 100;
        public bool IsDead = false;

        // 이동 속도, 현재 상태 이상 등 런타임에 변하는 모든 정보가 여기에 담깁니다.
    }
}