
using MessagePack;

namespace Script.Data
{
    [MessagePackObject]
    public struct RawMapNavData
    {
        [Key(0)]
        public ulong naviMask;

        [Key(1)]
        public uint infoMask;

        public RawMapNavData(ulong nav, uint info)
        {
            naviMask = nav;
            infoMask = info;
        }
    }
}