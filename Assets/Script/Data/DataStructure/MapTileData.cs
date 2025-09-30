
namespace Script.Data
{
    using MessagePack;
    using Unity.Collections;

    [MessagePackObject]
    public struct MapTileData
    {
        [Key(0)]
        [ReadOnly] public long NavMask;
        [Key(1)]
        [ReadOnly] public int LinkMask;

        public MapTileData(EditMapTileData edited)
        {
            NavMask = edited.NavMask;
            LinkMask = edited.LinkMask;
        }
    }
}
