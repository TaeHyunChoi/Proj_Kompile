using System.Collections.Generic;
using UnityEngine;
using CDataStructure;

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
public class Dev_MapSampler9th : MonoBehaviour
{
    [SerializeField] private Transform transformRsc;
    private Dictionary<int, Tile_t2> map;

    private void Awake()
    {
        map = new Dictionary<int, Tile_t2>();   
    }
    private void Start()
    {
        Dev_Tile[] tiles = transformRsc.GetComponentsInChildren<Dev_Tile>();
        for (int i = 0; i < tiles.Length; ++i)
        {
            tiles[i].Set(map);
        }

        foreach (int key in map.Keys)
        {
            PTile.DebugTileData(key, map[key]);
        }
    }
}
#endif