namespace Script.Unit.Entity
{
    using UnityEngine;

    /// <summary> GameObject에 부착되어 유닛의 애니메이션(Animator 상태 및 스프라이트 제어)을 전담 </summary>
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class UnitAnimComponent : MonoBehaviour
    {
        private UnitEntityBase _ownerEntity;

        [SerializeField] private Animator _animator;

        // 문자열 캐싱을 통한 가비지(GC) 할당 방지 및 탐색 속도 최적화
        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashDirX = Animator.StringToHash("DirX");
        private static readonly int HashDirZ = Animator.StringToHash("DirZ");
        private static readonly int HashHit = Animator.StringToHash("Hit");
        private static readonly int HashDead = Animator.StringToHash("Dead");
        private static readonly int HashAtk = Animator.StringToHash("Attack");

        public void Initialize(UnitEntityBase owner)
        {
            _ownerEntity = owner;

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
        }

        public void ManualUpdate()
        {
            // 애니메이션 상태를 매 프레임 내부적으로 갱신해야 할 일이 있다면 처리합니다.
            // 보통은 MoveComponent 등에서 명시적으로 UpdateMovementAnim을 호출하는 구조를 선호합니다.
        }

        // ===================================================================================
        // 외부 제어 인터페이스 (Entity가 호출)
        // ===================================================================================

        /// <summary>
        /// 이동 속도와 2.5D 환경에 맞춘 바라보는 방향을 업데이트합니다.
        /// </summary>
        public void UpdateMovementAnim(float speed, Vector3 direction)
        {
            if (_animator == null) return;

            _animator.SetFloat(HashSpeed, speed);

            // 속도가 있을 때만 방향 파라미터 업데이트 (2.5D 8방향/4방향 블렌드 트리 대응)
            if (speed > 0.01f)
            {
                _animator.SetFloat(HashDirX, direction.x);
                _animator.SetFloat(HashDirZ, direction.z);
            }
        }

        public void PlayAttackAnimation()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(HashAtk);
            }
        }

        public void PlayHitAnimation()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(HashHit);
            }
        }

        public void PlayDeathAnimation()
        {
            if (_animator != null)
            {
                _animator.SetBool(HashDead, true);
            }
        }

        // ===================================================================================
        // Animation Event 수신 (AnimationClip 설정용)
        // ===================================================================================

        /// <summary>
        /// 공격 애니메이션 도중 실제 타격(데미지 판정)이 들어가는 프레임에서 호출됩니다.
        /// </summary>
        public void OnAttackStrikeEvent()
        {
            if (_ownerEntity == null) return;

            // 예시: Entity를 통해 Manager에게 공격이 적중했음을 알리거나 데미지 계산을 트리거합니다.
            // _ownerEntity.ExecuteAttackHitLogic(); 
        }
    }
}