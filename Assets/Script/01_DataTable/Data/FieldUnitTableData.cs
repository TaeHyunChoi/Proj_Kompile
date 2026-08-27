namespace Kompile.Data
{
    using Kompile.Provider;
    using MessagePack;
    using Unity.Collections;
    using UnityEngine;

    /// <summary> 기획 데이터 (CSV 등) 1줄에 해당하는 순수 데이터 정의. 메모리 복사 비용 및 GC 방지를 위해 struct로 정의 </summary>
    [MessagePackObject]
    public struct FieldUnitTableData
    {
        [Key(0)]
        public int                  Index;
        [Key(1)]
        public FixedString32Bytes   NameKey;
        [Key(2)]
        public ActorBrainType        BrainType;
        [Key(3)]
        public float                CollisionRange;

        public async Awaitable<FieldActorAnimClip> GetAnimClipsAsync()
        {
            var clips = await AssetProvider.LoadFieldUnitAnimClipSetAsync(NameKey.ToString());
            return clips;
        }
    }
}