
namespace Script.Data
{
    public struct MapNavData
    {
        private ulong naviMask;
        private uint infoMask;

        public MapNavData(ulong nav, uint info)
        {
            naviMask = nav;
            infoMask = info;
        }
    }
}