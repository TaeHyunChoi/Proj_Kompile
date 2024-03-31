using System.Collections.Generic;
using UnityEngine;
using CDataStructure;
using System.Threading;

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
        DEV_Tile[] tiles = transformRsc.GetComponentsInChildren<DEV_Tile>();

        //## Set Tile Data
        //First set the information of each tile, then receive the information of surrounding tiles and link them.
        for (int i = 0; i < tiles.Length; ++i)
        {
            tiles[i].SetData(map);
        }

        //## Set Tile Link
        //In previous versions, it was searched using BFS.
        //But in some cases the space was 'disconnected' in the same layer, so it was used foreach.
        for (int i = 0; i < tiles.Length; ++i)
        {
            tiles[i].SetLink(map);
        }

        //PTile.DebugTileData(key, map[key]);
    }
}
#endif