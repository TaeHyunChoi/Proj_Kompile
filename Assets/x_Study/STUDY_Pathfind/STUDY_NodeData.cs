namespace Study.Pathfind
{
#if UNITY_EDITOR
    using MessagePack;
    using UnityEngine;
    using Unity.Collections;
    using Unity.Mathematics;

    [MessagePackObject]
    public sealed class STUDY_NodeData
    {
        [Key(0), ReadOnly]
        public long ID;         // computed from gKey, tKey
        [Key(1), ReadOnly]
        public ushort LinkMask; // 2 bits per direction
    
        public Vector3 ComputePosition()
        { 
            return STUDY_PositionKeyUtil.ComputeWorldPosition(ID);
        }
        public int3 ComputeAbsPosition()
        { 
            return STUDY_PositionKeyUtil.ComputeAbsoluteWorldPosition(ID);
        }
    }
#endif
}