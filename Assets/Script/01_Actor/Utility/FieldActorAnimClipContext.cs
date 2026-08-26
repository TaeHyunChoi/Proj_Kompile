namespace Kompile.Data
{
    using UnityEngine;

    /// <summary> 고정 인덱스 기반으로 액터의 클립들을 보관 </summary>
    public struct FieldActorAnimClipContext
    {
        public string UnitKey;
        public AnimationClip[] Clips;
    }
}