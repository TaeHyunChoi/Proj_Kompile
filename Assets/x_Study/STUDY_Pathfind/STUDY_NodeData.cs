namespace Study.Pathfind
{
    using MessagePack;
#if UNITY_EDITOR
    using Unity.Collections;
    using Unity.Mathematics;

    [MessagePackObject]
    public sealed class STUDY_NodeData
    {
        [Key(0), ReadOnly]
        public long ID;         // computed from gKey, tKey
        [Key(1), ReadOnly]
        public ushort LinkMask; // 2 bits per direction

        //public int3 AbsTile;    // optional cached absloute tile coordinates (filled by baker)
        public int3 ComputeAbsPosition()
        { 
            return STUDY_PositionKeyUtil.ComputeAbsoluteWorldPosition(ID);
        }
    }
#endif
}