namespace Study.Pathfind
{
    using Unity.Mathematics;

    public static class STUDY_LinkMaskUtil
    {
        // N:orth, S:outh, W:est, E:est
        public static readonly int3[] DirOffsets = new int3[] {
        new int3(-1, 0,-1), // N, W
        new int3( 0, 0,-1), // N
        new int3( 1, 0,-1), // N, E
        new int3( 1, 0, 0), // E
        new int3( 1, 0, 1), // S, E
        new int3( 0, 0, 1), // S
        new int3(-1, 0, 1), // S, W
        new int3(-1, 0, 0)  // W
    };

        public static bool TryGetYOffset(ushort mask, int dir, out int yOffset)
        {
            int v = (mask >> (dir * 2)) & 0b11;

            switch (v)
            {
                case 0b_00: yOffset = 0; return true;
                case 0b_01: yOffset = 1; return true;
                case 0b_10: yOffset = -1; return true;
                default:
                    break;
            }

            yOffset = 0;
            return false;
        }
    }

}