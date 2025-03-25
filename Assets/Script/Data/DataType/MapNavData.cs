
using MessagePack;

namespace Script.Data
{
    [MessagePackObject]
    public struct MapNavData
    {
        [Key(0)]
        public ulong naviMask;

        [Key(1)]
        public uint infoMask;

        public MapNavData(ulong nav, uint info)
        {
            naviMask = nav;
            infoMask = info;
        }
    }
}