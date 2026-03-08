namespace Script.Data
{
    using Script.Map.Data;
    using MessagePack;
    using Unity.Collections;

    [MessagePackObject]
    public struct MapTileData
    {
        [ReadOnly, Key(0)] public long NaviMask;
        [ReadOnly, Key(1)] public ushort LinkMask;
        [ReadOnly, Key(2)] public ushort LayerMask; // TODO: 추후에 추가 예정
        
#if UNITY_EDITOR
        public MapTileData(EditMapTileData edited)
        {
            NaviMask  = edited.NaviMask;
            LinkMask  = edited.LinkMask;
            LayerMask = 0;
        }
#endif
    }
}