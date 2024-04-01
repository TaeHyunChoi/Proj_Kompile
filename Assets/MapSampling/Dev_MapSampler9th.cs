using System.Collections.Generic;
using UnityEngine;
using DevDataType;
using System.Threading;

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
public class Dev_MapSampler9th : MonoBehaviour
{
    [SerializeField] private Transform transformRsc;
    private Dictionary<int, Tile_sample> sample;

    private void Awake()
    {
        sample = new Dictionary<int, Tile_sample>();
    }
    private void Start()
    {
        //## Set Tile Data
        //First set the information of each tile, then receive the information of surrounding tiles and link them.
        DEV_MapObj[] objects = transformRsc.GetComponentsInChildren<DEV_MapObj>();
        Tile_sample[] tiles;

        List<int> keys = new List<int>();
        for (int i = 0; i < objects.Length; ++i)
        {
            if (false == objects[i].TryGetTileArray(out tiles))
            {
                continue;
            }

            for (int t = 0; t < tiles.Length; ++t)
            {
                Tile_sample tile = tiles[t];
                int key = tile.Key;

                if (-1 == key)
                {
                    break;
                }
                if (true == sample.ContainsKey(key))
                {
                    continue;
                }

                sample.Add(key, tile);
                keys.Add(key);
                Debug.Log($"{key}:{PTile.GetPivot(key, tile.Scale)} (scale:{tile.Scale})");
            }
        }



        for(int i = 0; i < keys.Count; ++i)
        {
            int keyMy = keys[i];
            for (int indexLink = 0; indexLink < 12; ++indexLink)
            {
                for (int y = -1; y <= 1; ++y)
                {
                    switch (indexLink)
                    {
                        //Diagonal direction
                        //case 0:
                        //    break;
                        //case 3:
                        //    break;
                        //case 6:
                        //    break;
                        //case 9:
                        //    break;
                        case 0:
                        case 3:
                        case 6:
                        case 9:
                            continue;

                        //Right direction
                        default:
                            if (false == PTile.IsLinkableWith(sample, keyMy, indexLink, y))
                            {
                                continue;
                            }
                            break;
                    }

                    //set link data
                    int flagLink;
                    switch (y)
                    {
                        case  0: flagLink = 0b01 << (indexLink * 2); break;
                        case  1: flagLink = 0b10 << (indexLink * 2); break;
                        case -1: flagLink = 0b11 << (indexLink * 2); break;
                        default: continue;
                    }

                    Tile_sample tile = sample[keyMy];
                    int key = tile.Key;
                    int info = tile.Info | flagLink;
                    int move = tile.Move;
                    int height = tile.Height;

                    sample[keyMy] = new Tile_sample(key, info, move, height);
                    Debug.Log($"pivot:{PTile.GetPivot(keyMy, tile.Scale)}(scale:{tile.Scale}) link[{System.Convert.ToString(sample[keyMy].Link, 2)}]");
                }
            }
        }
    }
}
#endif