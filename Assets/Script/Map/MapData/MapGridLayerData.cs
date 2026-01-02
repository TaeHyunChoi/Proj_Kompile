using MessagePack;
using System.Collections.Generic;
using Unity.Collections;

[MessagePackObject]
public class MapGridLayerData
{
    [Key(0), ReadOnly]
    public int layer;

    [Key(1), ReadOnly]
    public List<string> assets;

    public MapGridLayerData() { }
    public MapGridLayerData(int _layer, string asset)
    {
        layer = _layer;
        assets = new List<string>() { asset };
    }
    public void Add(string asset)
    {
        assets.Add(asset);
    }
}
