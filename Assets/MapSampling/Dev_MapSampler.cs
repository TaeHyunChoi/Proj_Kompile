using System.Collections.Generic;
using UnityEngine;
using DataType;
using CMathf;

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
public class Dev_MapSampler : MonoBehaviour
{
    [SerializeField] 
    private Transform transformRsc;
    private static Dictionary<int, Tile_t> map = new Dictionary<int, Tile_t>();

    private void Start()
    {
        // initialize tiles
        List<int> keys = new List<int>();
        foreach (int k in map.Keys)
        {
            keys.Add(k);
        }

        //set link: right direction 
        int[] quarants = new int[] { -1,  0,  4, -1, 5, 13, -1, 14, 10, -1, 11, 3,
                                     -1, 10, 14, -1, 3, 11, -1,  4,  0, -1, 13, 5};
        for (int k = 0; k < keys.Count; ++k)
        {
            int keyMy = keys[k];
            Tile_t tileMy = map[keyMy];
            for (int i = 0; i < 12; ++i)
            {
                //diagonal : later
                if (0 == i % 3)
                {
                    continue;
                }

                int qMy = quarants[i];
                int qTarget = quarants[i + 12];

                if (false == tileMy.IsMovable(qMy))
                {
                    continue;
                }
                int keyNeighbor = GetRightLinkedKey(keyMy, i);
                for (int y = -1; y <= 1; ++y)
                {
                    int key = keyNeighbor + y * (1 << 8); //TODO: �̰� scale ������ �� �ְڴ�?

                    if (false == map.TryGetValue(key, out Tile_t tileLinked))
                    {
                        continue;
                    }
                    if (false == tileLinked.IsMovable(qTarget))
                    {
                        continue;
                    }

                    byte hMy0, hMy1, hNei0, hNei1;
                    switch (qMy)
                    {
                        case  0: hMy0 = 0; hMy1 = 1; hNei0 = 6; hNei1 = 7; break;
                        case  4: hMy0 = 1; hMy1 = 2; hNei0 = 7; hNei1 = 8; break;
                        case  5: hMy0 = 2; hMy1 = 5; hNei0 = 0; hNei1 = 3; break;
                        case 13: hMy0 = 5; hMy1 = 8; hNei0 = 3; hNei1 = 6; break;
                        case 14: hMy0 = 8; hMy1 = 7; hNei0 = 2; hNei1 = 1; break;
                        case 10: hMy0 = 7; hMy1 = 6; hNei0 = 1; hNei1 = 0; break;
                        case 11: hMy0 = 6; hMy1 = 3; hNei0 = 8; hNei1 = 5; break;
                        case  3: hMy0 = 3; hMy1 = 0; hNei0 = 5; hNei1 = 2; break;
                        default: continue;
                    }

                    if (tileMy.GetYValue(keyMy, hMy0) != tileLinked.GetYValue(key, hNei0))
                    {
                        continue;
                    }
                    if (tileMy.GetYValue(keyMy, hMy1) != tileLinked.GetYValue(key, hNei1))
                    {
                        continue;
                    }

                    //set data
                    int flag = 0;
                    switch (y)
                    {
                        case  0: flag = 0b01 << i * 2; break;
                        case  1: flag = 0b10 << i * 2; break;
                        case -1: flag = 0b11 << i * 2; break;
                    }

                    int info      = tileMy.Info | flag;
                    long movement = tileMy.Movement;
                    map[keyMy]    = tileMy = new Tile_t(info, movement);
                    break;
                }

            }
        }

        //set link: diagonal direction 
        for (int k = 0; k < keys.Count; ++k)
        {
            int keyMy = keys[k];
            Tile_t tileMy = map[keyMy];

            //(1.00, 0.00, 1.00)
            Vector3 pivot = PTile.GetPivot(keyMy, tileMy.Scale);

            for (int i = 0; i < 12; i += 3)
            {
                int maskLink, sign;
                int keyRoute;
                Tile_t tileRoute;

                byte index00, index01, index10, index11; //link index
                switch (i)
                {
                    case 0: index00 =  1; index01 = 10; index10 = 11; index11 =  2; break;
                    case 3: index00 =  2; index01 =  5; index10 =  4; index11 =  1; break;
                    case 6: index00 =  5; index01 =  8; index10 =  9; index11 =  4; break;
                    case 9: index00 =  8; index01 = 11; index10 = 10; index11 =  7; break;
                    default: continue;
                }

                byte loop = 0;
                while (loop < 2)
                {
                    byte index0, index1;

                    if (0 == loop) 
                    { 
                        index0 = index00;
                        index1 = index01;
                    }
                    else //if (1 == loop) 
                    {
                        index0 = index10;
                        index1 = index11;
                    }
                    ++loop;

                    if (false == tileMy.IsLinked(index0))
                    {
                        continue;
                    }
                    maskLink = (tileMy.Link >> (index0 * 2)) & 0b11;

                    sign = 0;
                    switch (maskLink)
                    {
                        case 0b01: sign =  0; break;
                        case 0b10: sign =  1; break;
                        case 0b11: sign = -1; break;
                    }

                    keyRoute = keyMy + sign * (1 << 8);
                    switch (index0)
                    {
                        case 1:
                        case 2:
                            keyRoute +=  (0 << 16) - (1 << 0);
                            break;
                        case 4:
                        case 5:
                            keyRoute +=  (1 << 16) + (0 << 0); 
                            break;
                        case 8:
                        case 9:
                            keyRoute +=  (0 << 16) + (1 << 0); 
                            break;
                        case 10:
                        case 11: 
                            keyRoute += -(1 << 16) + (0 << 0); 
                            break;
                        default: continue;
                    }

                    if (false == map.TryGetValue(keyRoute, out tileRoute))
                    {
                        continue;
                    }
                    if (true == tileRoute.IsLinked(index1))
                    {
                        //TODO: Get Y Flag (pivotMy.y, pivotTarget.y)
                        int flagLink = maskLink << (i * 2);

                        int info      = tileMy.Info | flagLink;
                        long movement = tileMy.Movement;
                        map[keyMy]    = tileMy = new Tile_t(info, movement);
                        break;
                    }
                }
            }
        }

        //debug.log
        for (int k = 0; k < keys.Count; ++k)
        {
            int key = keys[k];
            Tile_t tile = map[key];

            string move   = System.Convert.ToString(tile.Move, 2).ToString();
            string height = System.Convert.ToString(tile.Height, 2).ToString();
            string link   = System.Convert.ToString(tile.Info & 0xFFFFFF, 2);
            Debug.Log($"{PTile.GetPivot(key, tile.Scale)} m:{move} l:{link}\nh:{height}");
        }

        DataTable.WriteBinaryMappingData<Tile_t>(map, "test_map");
    }

    public  static void InitTile(Transform transform, Mesh mesh, float scale, byte layer, byte status)
    {
        int info = status << 24;

        Quaternion rot = transform.rotation;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;

        for (int t = 0; t < triangles.Length; t += 3)
        {
            int t0 = triangles[t];
            int t1 = triangles[t + 1];
            int t2 = triangles[t + 2];

            //Determine whether the mesh is the target for sampling by normal value.
            Vector3 normal1 = rot * normals[t0];
            Vector3 normal2 = rot * normals[t1];
            Vector3 normal3 = rot * normals[t2];
            Vector3 normal = normal1;
            if (normal2.y < normal.y) { normal = normal2; }
            if (normal3.y < normal.y) { normal = normal3; }
            normal = CMath.FloorToVector(normal, 3);
            if (0 >= normal.y)
            {
                continue;
            }

            float size_half = PTile.GetSize(TileSize.Half, scale);
            Vector3 A = PTile.SnappingPoint(transform.TransformPoint(vertices[t0]), size_half, 3);
            Vector3 B = PTile.SnappingPoint(transform.TransformPoint(vertices[t1]), size_half, 3);
            Vector3 C = PTile.SnappingPoint(transform.TransformPoint(vertices[t2]), size_half, 3);

            SetTileData(A, B, C, scale, layer, info);
        }
    }
    private static void SetTileData(Vector3 p0, Vector3 p1, Vector3 p2, float scale, byte layer, int info)
    {
        //(Isosceles right triangle) Find the right angle point and store it in p0
        float v0to1 = CMath.Floor(Vector3.Distance(new Vector3(p0.x, 0, p0.z), new Vector3(p1.x, 0, p1.z)), 3);
        float v1to2 = CMath.Floor(Vector3.Distance(new Vector3(p1.x, 0, p1.z), new Vector3(p2.x, 0, p2.z)), 3);
        float v0to2 = CMath.Floor(Vector3.Distance(new Vector3(p0.x, 0, p0.z), new Vector3(p2.x, 0, p2.z)), 3);

        float diagonal = v1to2;
        Vector3 swap;
        if (diagonal < v0to1)
        {
            swap = p2;
            p2 = p0;
            p0 = swap;

            diagonal = v0to1;
        }
        if (diagonal < v0to2)
        {
            swap = p1;
            p1 = p0;
            p0 = swap;

            diagonal = v0to2;
        }

        float size_half   = PTile.GetSize(TileSize.Half,   scale);
        float size_quater = PTile.GetSize(TileSize.Quater, scale);

        if (size_half < diagonal)
        {
            Vector3 midPoint = PTile.SnappingPoint((p1 + p2) * 0.5f, size_quater, 3);
            SetTileData(p0, p1, midPoint, scale, layer, info);
            SetTileData(p0, p2, midPoint, scale, layer, info);
        }
        else
        {
            float size = PTile.GetSize(TileSize.Default, scale);

            //get point, get pivot
            Vector3 pointCenter = PTile.SnappingPoint((p0 + p1 + p2) * 0.333f, size_half, 3);
            Vector3 pivot = PTile.GetPivot(pointCenter, size);

            //set flag
            int move = GetMoveFlag(pointCenter - pivot, size);

            long height = 0;
            float size_quater_inverse = PTile.GetSize(TileSize.Quater_inverse, scale);
            height |= GetHeightFlag(p0 - pivot, size_quater, size_quater_inverse);
            height |= GetHeightFlag(p1 - pivot, size_quater, size_quater_inverse);
            height |= GetHeightFlag(p2 - pivot, size_quater, size_quater_inverse);

            //set tile data
            int key = (layer << 24) | PTile.GetKey(pointCenter, size);
            if (false == map.TryGetValue(key, out Tile_t tile))
            {
                map.Add(key, new Tile_t(info, move, height));
            }
            else
            {
                info   |= tile.Info;
                move   |= tile.Move;
                height |= tile.Height;
                map[key] = new Tile_t(info, move, height);
            }
        }
    }
    private static int GetMoveFlag(Vector3 diff, float size)
    {
        float size_half = size * 0.5f;

        int quarant = 0;
        if (diff.x >= size_half)
        {
            quarant |= 0b_01;
            diff -= new Vector3(size_half, 0, 0);
        }
        if (diff.z >= size_half)
        {
            quarant |= 0b_10;
            diff -= new Vector3(0, 0, size_half);
        }
        quarant *= 4;

        int equation = 0;
        if (diff.z >= diff.x)
        {
            equation |= 0b01;
        }
        if (diff.z >= -diff.x + size_half)
        {
            equation |= 0b10;
        }

        switch (equation)
        {
            case 0b00: return 1 << (0 + quarant);
            case 0b10: return 1 << (1 + quarant);
            case 0b11: return 1 << (2 + quarant);
            case 0b01: return 1 << (3 + quarant);
        }

        return -1;
    }
    private static long GetHeightFlag(Vector3 diff, float size_quater, float size_quater_inverse)
    {
        diff = PTile.SnappingPoint(diff, size_quater, 3);
        int x  = (int)(diff.x * size_quater_inverse);
        long y = (long)(diff.y * size_quater_inverse);  //y: 0 ~ 4 (0b000 ~ 0b100)
        int z  = (int)(diff.z * size_quater_inverse);

        int shift;
        switch (x * 10 + z)
        {
            case 00: shift =  0; break;
            case 20: shift =  1; break;
            case 40: shift =  2; break;
            case 02: shift =  3; break;
            case 22: shift =  4; break;
            case 42: shift =  5; break;
            case 04: shift =  6; break;
            case 24: shift =  7; break;
            case 44: shift =  8; break;
            case 11: shift =  9; break;
            case 31: shift = 10; break;
            case 13: shift = 11; break;
            case 33: shift = 12; break;
            default:
                Debug.LogError($"{diff:F3} {x},{z} => {y}");
                return 0;
        }
        shift *= 3;

        return y << shift;
    }

    private int GetRightLinkedKey(int key, int indexLink)
    {
        int keyNeighbor = -1;
        switch (indexLink)
        {
            case 1:
            case 2:
                keyNeighbor = key + (0 << 16) - (1 << 0);
                break;
            case 4:
            case 5:
                keyNeighbor = key + (1 << 16) + (0 << 0);
                break;
            case 7:
            case 8:
                keyNeighbor = key + (0 << 16) + (1 << 0);
                break;
            case 10:
            case 11:
                keyNeighbor = key - (1 << 16) + (0 << 0);
                break;
        }

        return keyNeighbor;
    }
}
#endif