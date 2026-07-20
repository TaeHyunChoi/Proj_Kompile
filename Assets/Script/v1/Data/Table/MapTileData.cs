namespace Kompile.Data
{
    using MessagePack;
    using Unity.Collections;

    [MessagePackObject]
    public struct MapTileData
    {
        [ReadOnly, Key(0)] public long NaviMask;
        [ReadOnly, Key(1)] public ushort LinkMask;
        [ReadOnly, Key(2)] public ushort LayerMask;

#if UNITY_EDITOR
        public MapTileData(long naviMask, ushort linkMask, ushort layerMask)
        {
            NaviMask  = naviMask;
            LinkMask  = linkMask;
            LayerMask = layerMask;
        }
#endif
    }
}