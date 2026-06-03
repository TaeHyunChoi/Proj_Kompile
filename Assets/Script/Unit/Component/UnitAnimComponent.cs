namespace Kompile.Unit.Component
{
    using System.Collections.Generic;
    using UnityEngine;
    using Kompile.Unit.Data;

    /// <summary> GameObject에 부착되어 유닛의 애니메이션(Animator 상태 및 스프라이트 제어)을 전담 </summary>
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class UnitAnimComponent : MonoBehaviour
    {
        private static readonly int HashDirX = Animator.StringToHash("DirX");
        private static readonly int HashDirZ = Animator.StringToHash("DirZ");

        private Animator _animator;
        private AnimatorOverrideController _runtimeAOC;

        // 캐시된 리스트를 활용해 GC Alloc을 원천 봉쇄합니다.
        private readonly List<KeyValuePair<AnimationClip, AnimationClip>> _templatedPairCached = new(8);

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
            ApplyRuntimeClips(in clipSet);
            _animator.runtimeAnimatorController = _runtimeAOC;

            // 4. 상태 재바인딩 및 초기화
            _animator.Rebind();
            _animator.Update(0f);
        }

        private void ApplyRuntimeClips(in FieldUnitAnimClipContext clipSet)
        {
            if (!_runtimeAOC || null == clipSet.Clips)
            {
                return;
            }

            // 가비지 없이 현재 매핑 정보 리스트 가져오기
            _templatedPairCached.Clear();
            _runtimeAOC.GetOverrides(_templatedPairCached);

            // 리스트 데이터를 직접 수정 (KeyValuePair는 구조체이므로 새로 생성하여 대입)
            int maxCount = Mathf.Min(_templatedPairCached.Count, clipSet.Clips.Length);
            for (int i = 0; i < maxCount; ++i)
            {
                AnimationClip originalClip = _templatedPairCached[i].Key;
                AnimationClip overrideClip = clipSet.Clips[i];

                _templatedPairCached[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, overrideClip);
            }

            _runtimeAOC.ApplyOverrides(_templatedPairCached);
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
            if (speed > 0.01f)
            {
                _animator.SetFloat(HashDirX, direction.x);
                _animator.SetFloat(HashDirZ, direction.z);
            }
        }
    }
}