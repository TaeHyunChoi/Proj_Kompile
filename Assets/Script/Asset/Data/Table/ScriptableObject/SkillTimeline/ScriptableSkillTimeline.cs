namespace Kompile.Asset.Data
{
    using UnityEngine;

    /// <summary>
    /// 스킬의 각 단계별 타임라인과 애니메이션 연결 정보를 담는 순수 데이터 구조체입니다.
    /// 힙 메모리 할당(GC)을 방지하기 위해 class가 아닌 struct로 정의됩니다.
    /// </summary>
    [System.Serializable]
    public struct ActionTimelineData
    {
        [Header("Frame Settings (60 FPS Base)")]
        [Tooltip("공격을 준비하는 대기 프레임 수입니다.")]
        public int PreActionFrames;

        [Tooltip("실제 스킬이 발동되고 타격이 이루어지는 프레임 수입니다.")]
        public int ActionFrames;

        [Tooltip("공격 후 자세를 가다듬는 후딜레이 프레임 수입니다.")]
        public int PostActionFrames;

        [Header("Animation Keys (Addressables)")]
        [Tooltip("준비 동작 애니메이션의 Addressables Key")]
        public string PreActionAnimKey;

        [Tooltip("타격 동작 애니메이션의 Addressables Key")]
        public string ActionAnimKey;

        [Tooltip("후딜레이 동작 애니메이션의 Addressables Key")]
        public string PostActionAnimKey;

        [Header("Combat Rules")]
        [Tooltip("True일 경우 Pre-Action 도중 피격 시 스킬이 취소(Interrupt)됩니다.")]
        public bool IsInterruptibleDuringPreAction;
    }

    [CreateAssetMenu(fileName = "ScriptableSkillTimeline", menuName = "Scriptable Objects/ScriptableSkillTimeline")]
    public class ScriptableSkillTimeline : ScriptableObject
    {
        [SerializeField]
        [Tooltip("구글 스프레드시트나 CSV 등 외부 테이블과 연동하기 위한 고유 식별자입니다.")]
        private int skillID;

        [SerializeField]
        private ActionTimelineData timelineData;

        // 외부(Manager나 Provider)에서 접근할 때 원본 데이터를 보호하고 
        // 값 복사(Value Copy) 형태로 안전하게 넘겨주기 위한 프로퍼티입니다.
        public int SkillID => skillID;
        public ActionTimelineData TimelineData => timelineData;
    }
}