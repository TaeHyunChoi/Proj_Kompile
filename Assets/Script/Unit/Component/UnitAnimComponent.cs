namespace Kompile.Unit.Component
{
    using System.Collections.Generic;
    using UnityEngine;
    using Kompile.Unit.Data;
    using Kompile.Asset.Provider;

    /// <summary> GameObject에 부착되어 유닛의 애니메이션(Animator 상태 및 스프라이트 제어)을 전담 </summary>
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class UnitAnimComponent : MonoBehaviour
    {
        private static readonly int HashDirX = Animator.StringToHash("DirX");
        private static readonly int HashDirZ = Animator.StringToHash("DirZ");
        private static readonly int HashIsMoving = Animator.StringToHash("IsMoving");

        private Animator _animator;
        private AnimatorOverrideController _runtimeAOC;

        public void Initialize(AnimatorOverrideController baseTemplateAOC, in FieldUnitAnimClipContext clipSet)
        {
            if (baseTemplateAOC == null)
            {
                return;
            }

            _animator = transform.GetComponent<Animator>();

            // 1. 템플릿 복사본을 만들어 독점적 인스턴스 확보
            _runtimeAOC = new AnimatorOverrideController(baseTemplateAOC);

            // 2. Animator에 연결하기 '전'에 클립들을 일괄 오버라이드 (애니메이션 미출력 버그 방지)
            // 3. 수정이 완료된 컨트롤러를 런타임 컨트롤러에 할당
            AssetProvider.ApplyOverrideClips(_runtimeAOC, in clipSet);
            _animator.runtimeAnimatorController = _runtimeAOC;

            // 4. 상태 재바인딩 및 초기화
            _animator.Rebind();
            _animator.Update(0f);
        }

        /// <summary> Entity.Update()에서 UnitIntent를 수신하여 호출 </summary>
        public void UpdateIntent(in UnitIntent intent)
        {
            float speed = intent.MoveInput.magnitude;
            Vector3 dir = new Vector3(intent.MoveInput.x, 0f, intent.MoveInput.y);
            UpdateMovementAnim(speed, dir);
        }

        /// <summary> 이동 속도와 2.5D 환경에 맞춘 바라보는 방향을 업데이트 </summary>
        private void UpdateMovementAnim(float speed, Vector3 direction)
        {
            // 2. 현재 이동 중인지 여부 체크 (임계값 0.01f)
            bool isMoving = speed > 0.01f;

            // 3. Animator에 이동 상태 전달
            _animator.SetBool(HashIsMoving, isMoving);

            // 4. 이동 중일 때만 방향 정보를 업데이트하여, 멈췄을 때는 마지막 방향(DirX, DirZ)을 유지하도록 함
            if (isMoving)
            {
                _animator.SetFloat(HashDirX, direction.x);
                _animator.SetFloat(HashDirZ, direction.z);
            }
        }
    }
}