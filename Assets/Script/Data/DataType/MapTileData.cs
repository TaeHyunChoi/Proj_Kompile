namespace Script.Data
{
    using MessagePack;

    [MessagePackObject]
    public struct MapTileData
    {
        [Key(0)]
        public long naviMask; // [layer:3bits], [heights:52bits(4*13)]

        [Key(1)]
        public uint infoMask;

        public MapTileData(long nav, uint info)
        {
            naviMask = nav;
            infoMask = info;
        }
    }

    public readonly struct IngameMapTileData
    {
        public readonly int GridKey;
        public readonly int TileKey;
        public readonly long NaviMask;
        public readonly uint InfoMask;

        public IngameMapTileData(int g, int t, MapTileData data)
        {
            GridKey = g;
            TileKey = t;
            NaviMask = data.naviMask;
            InfoMask = data.infoMask;
        }
    }
}