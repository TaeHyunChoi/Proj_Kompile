using System.Collections.Generic;
using UnityEngine;
using MessagePack;

[MessagePackObject]
public class PathNodeData
{
    [Key(0)]
    public long ID; // (gKey << 32) | tKey;

    [Key(1)]
    public Vector3 Pivot; // 얘도 ID로 구할 수 있고...

    [Key(2)]
    public List<long> ConnectedNodeIDs; // .LinkMask로 들고 있는게 좋을 것 같기도;

    public PathNodeData() { }

    public PathNodeData(long id, Vector3 pivot)
    {
        ID    = id;
        Pivot = pivot;
        ConnectedNodeIDs = new List<long>();
    }
}
