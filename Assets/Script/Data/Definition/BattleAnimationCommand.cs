namespace Kompile.Data
{
    public struct BattleAnimationCommand
    {
        public int StateHash;
        public int StartFrame;

        public int StartupTicks;
        public int ActiveTicks;
        public int RecoveryTicks;

        public int HitFrameOffset;
        public int ComboWindow;

        public int TotalTicks => StartupTicks + ActiveTicks + RecoveryTicks;
    }
}