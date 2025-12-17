using System.Collections.Generic;
using UnityEngine;
using MessagePack;

[MessagePackObject]
public class PathNodeData
{
    [Key(0)]
    public int ID;

    [Key(1)]
    public string Name;

    [Key(2)]
    public Vector3 Pivot;

    [Key(3)]
    public List<int> ConnectedNodeIDs;

    public PathNodeData() { }

    public PathNodeData(int id, string name, Vector3 pivot)
    {
        ID    = id;
        Name  = name;
        Pivot = pivot;
        ConnectedNodeIDs = new List<int>();
    }
}
