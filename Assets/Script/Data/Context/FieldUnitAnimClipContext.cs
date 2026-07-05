namespace Kompile.Data
{
    using UnityEngine;

    /// <summary> 고정 인덱스 기반으로 유닛의 클립들을 보관하는 구조체 (값 중심) </summary>
    public struct FieldUnitAnimClipContext
    {
        public string UnitKey;
        public AnimationClip[] Clips;
    }
}