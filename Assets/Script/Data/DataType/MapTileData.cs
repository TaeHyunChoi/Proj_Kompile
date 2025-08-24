
using MessagePack;

namespace Script.Data
{
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

        public bool IsValid()
        {
            return 0 <= naviMask;
        }
    }
}