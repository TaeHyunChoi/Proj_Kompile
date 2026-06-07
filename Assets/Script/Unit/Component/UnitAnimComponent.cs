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

            // 1. 현재 AOC가 가지고 있는 템플릿 오버라이드 구조를 가져옴
            _templatedPairCached.Clear();
            _runtimeAOC.GetOverrides(_templatedPairCached);

            // 2. 외부에 정의된 clipSet의 개수만큼 순회하며 일치하는 오리지널 클립을 찾아 교체
            // 💡 데이터(Data) 구조 내에 대상 오리지널 클립의 이름이나 식별자가 포함되어 있어야 안전합니다.
            int overrideCount = clipSet.Clips.Length;
            int templateCount = _templatedPairCached.Count;

            for (int i = 0; i < templateCount; ++i)
            {
                AnimationClip originalClip = _templatedPairCached[i].Key;
                if (originalClip == null) continue;

                // clipSet 구조 내에서 originalClip.name과 일치하는 런타임 클립이 있는지 검사
                for (int j = 0; j < overrideCount; ++j)
                {
                    // 예시: clipSet 내부 구조가 (OriginalName, TargetClip) 쌍이거나
                    // 혹은 타겟 클립의 네임 규칙이 오리지널을 포함하는 구조여야 합니다.
                    if (IsMatchingClip(originalClip.name, clipSet.Clips[j]))
                    {
                        _templatedPairCached[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, clipSet.Clips[j]);
                        break;
                    }
                }
            }

            // 3. 일괄 적용
            _runtimeAOC.ApplyOverrides(_templatedPairCached);
        }

        private bool IsMatchingClip(string originalName, AnimationClip targetClip)
        {
            if (targetClip == null) return false;

            // 네이밍 규칙에 맞게 매칭 (예: 오리지널이 "Warrior_Idle" 이고 타겟이 "Orc_Idle" 일 때 "Idle" 키워드로 매칭 등)
            // 가장 좋은 방법은 FieldUnitAnimClipContext에 애초에 매칭 정보가 포함되어 있는 것입니다.
            return targetClip.name.Contains(originalName);
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