using MessagePack;
using System.Collections.Generic;
using Unity.Collections;

[MessagePackObject]
public struct GridLayerData
{
    [Key(0), ReadOnly]
    public int layer;

    [Key(1), ReadOnly]
    public List<string> assets;

    public GridLayerData(int _layer, string asset)
    {
        layer = _layer;
        assets = new List<string>() { asset };
    }
    public readonly void Add(string asset)
    {
        assets.Add(asset);
    }
}
