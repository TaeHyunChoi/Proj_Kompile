
namespace Script.Data
{
    using MessagePack;
    using Unity.Collections;

    [MessagePackObject]
    public struct MapTileData
    {
        [ReadOnly, Key(0)]
        public long NavMask;
        
        [ReadOnly, Key(1)]
        public int LinkMask;

#if UNITY_EDITOR
        public MapTileData(EditMapTileData edited)
        {
            NavMask = edited.NaviMask;
            LinkMask = edited.LinkMask;
        }
#endif

        public MapTileData(long navMask)
        {
            NavMask  = navMask;
            LinkMask = 0xFFFF;
        }
    }
}
