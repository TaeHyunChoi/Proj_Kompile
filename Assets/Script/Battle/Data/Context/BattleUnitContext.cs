namespace Script.Battle.Data
{
    using UnityEngine;

    public enum BattlePhase
    {
        Wait,   // 10,000 -> 0 대기 단계
        Action  // 커맨드 입력 후 타격을 향한 실행 단계
    }
    
    /// <summary> 런타임 중 유닛의 거리와 페이즈를 추적하는 상태 객체 </summary>
    public class BattleUnitContext
    {
        public long EntityID { get; private set; }
        public int CurrentSpeed { get; set; }
        
        public float RemainingDistance { get; set; }
        public float ActionDistance { get; set; }
        public BattlePhase Phase { get; set; }

        public BattleUnitContext(long id, int baseSpeed)
        {
            EntityID = id;
            CurrentSpeed = baseSpeed;
            RemainingDistance = 10000f; // Target Distance로 초기화
            ActionDistance = 0f;
            Phase = BattlePhase.Wait;
        }
        
        // [넉백 및 속도 변화 반영]
        public void ApplyKnockback(float distance) => RemainingDistance += distance;
        public void SetSpeed(int newSpeed) => CurrentSpeed = newSpeed;
    }
}