namespace Kompile.Unit.Component
{
    using UnityEngine;
    using Kompile.Unit.Data;
    using Kompile.Unit.Entity;

    /// <summary> GameObject에 부착되어 유닛의 애니메이션(Animator 상태 및 스프라이트 제어)을 전담 </summary>
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class UnitAnimComponent : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private AnimatorOverrideController _overrideController;

        private UnitEntityBase _ownerEntity;
        
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
            _animator = GetComponentInChildren<Animator>();
            if (_overrideController != null)
                _animator.runtimeAnimatorController = _overrideController;
        }

        /// <summary>
        /// Entity.Update()에서 UnitIntent를 수신하여 호출합니다.
        /// 루프 상태(Idle/Walk)는 MoveInput 크기로 판단하고, 트리거성 명령(Attack/Hit/Dead)은 AnimCommand로 처리합니다.
        /// </summary>
        public void Update_(in UnitIntent intent)
        {
            float speed = intent.MoveInput.magnitude;
            Vector3 dir = new Vector3(intent.MoveInput.x, 0f, intent.MoveInput.y);
            UpdateMovementAnim(speed, dir);

            ApplyAnimCommand(intent.AnimCommand);
        }

        private void ApplyAnimCommand(UnitAnimCmd cmd)
        {
            switch (cmd)
            {
                case UnitAnimCmd.Attack: PlayAttackAnimation(); break;
                case UnitAnimCmd.Hit:    PlayHitAnimation();    break;
                case UnitAnimCmd.Dead:   PlayDeathAnimation();  break;
                case UnitAnimCmd.None:
                default:
                    break;
            }
        }

        // ===================================================================================
        // 외부 제어 인터페이스 (Entity가 호출)
        // ===================================================================================

        /// <summary>
        /// 이동 속도와 2.5D 환경에 맞춘 바라보는 방향을 업데이트합니다.
        /// </summary>
        public void UpdateMovementAnim(float speed, Vector3 direction)
        {   
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
            _animator.Play(HashAtk, 0, 0f);
        }

        public void PlayHitAnimation()
        {
            _animator.SetTrigger(HashHit);
        }

        public void PlayDeathAnimation()
        {
            _animator.SetBool(HashDead, true);
        }

        // ===================================================================================
        // Animation Event 수신 (AnimationClip 설정용)
        // ===================================================================================

        /// <summary>
        /// 공격 애니메이션 도중 실제 타격(데미지 판정)이 들어가는 프레임에서 호출됩니다.
        /// </summary>
        public void OnAttackStrikeEvent()
        {
            if (!_ownerEntity) 
                return;

            // 예시: Entity를 통해 Manager에게 공격이 적중했음을 알리거나 데미지 계산을 트리거합니다.
            // _ownerEntity.ExecuteAttackHitLogic(); 
        }
    }
}