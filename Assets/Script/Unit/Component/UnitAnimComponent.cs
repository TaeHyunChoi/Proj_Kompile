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
        private List<KeyValuePair<AnimationClip, AnimationClip>> _templatedPairCached = new List<KeyValuePair<AnimationClip, AnimationClip>>((int)FieldUnitAnimIndex.Count);

        public void Initialize(AnimatorOverrideController baseTemplateAOC, in FieldUnitAnimClipContext clipSet)
        {
            _animator = transform.GetComponent<Animator>();

            // 템플릿 복사본을 만들어 독점적 인스턴스 확보 (어드레서블 원본 보호)
            _runtimeAOC = new AnimatorOverrideController(baseTemplateAOC);
            _animator.runtimeAnimatorController = _runtimeAOC;

            // 클립 오버라이드 매핑
            ApplyRuntimeClips(baseTemplateAOC, in clipSet);
            _animator.Rebind();
            _animator.Update(0f);
        }
        private void ApplyRuntimeClips(AnimatorOverrideController templateAOC, in FieldUnitAnimClipContext clipSet)
        {
            if (!_runtimeAOC || !templateAOC || 
                null == clipSet.Clips)
            {
                return;
            }

            // 가비지 없이 목록 초기화
            _templatedPairCached.Clear();
            templateAOC.GetOverrides(_templatedPairCached);

            AnimationClip[] baseClips = templateAOC.animationClips;
            int maxCount = Mathf.Min(baseClips.Length, clipSet.Clips.Length);

            AnimationClip baseClip;
            for (int i = 0; i < maxCount; ++i)
            {
                baseClip = _templatedPairCached[i].Key;
                _runtimeAOC[baseClip] = clipSet.Clips[i];
            }
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
            // 속도가 있을 때만 방향 파라미터 업데이트 (2.5D 8방향/4방향 블렌드 트리 대응)
            if (speed > 0.01f)
            {
                _animator.SetFloat(HashDirX, direction.x);
                _animator.SetFloat(HashDirZ, direction.z);
            }
        }
    }
}