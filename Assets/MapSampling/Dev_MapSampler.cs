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
        //// initialize tiles
        List<int> keys = new List<int>();
        foreach (int k in map.Keys)
        {
            keys.Add(k);
        }

        //// set links
        int[] triangles = new int[] { 0, 4, 5, 13, 14, 10, 11, 3 };
        foreach (int key in keys)
        {
            //Vector3 pivot = PTile.GetPivot(key, map[key].Scale);

            for (int t = 0; t < 8; ++t)
            {
                int triangle = triangles[t];
                int linkMy = GetLinkIndex(triangle);
                int y;

                if (false == SetLink(key, triangle, linkMy, out int keyNext, out int triangleNext))
                {
                    continue;
                }

                int linkToLeft = (linkMy + 11) % 12;
                int linkToRight = (linkMy + 1) % 12;
                int linkOther;

                //differnet tile
                int quarant = (int)(triangleNext * 0.25f) * 4;
                if (0 == linkToLeft % 3)
                {
                    triangleNext = quarant + (triangleNext + 1) % 4;
                    int linkNext = GetLinkIndex(triangleNext);
                    linkOther = linkToRight;

                    if (true == SetLink(keyNext, triangleNext, linkNext, out int keyNextNext, out int not_used))
                    {
                        y = ((keyNextNext >> 8) & 0xFF) - ((key >> 8) & 0xFF); 
                        SetTile(key, linkToLeft, y);
                    }
                }
                else
                {
                    triangleNext = quarant + (triangleNext + 3) % 4;
                    int linkNext = GetLinkIndex(triangleNext);
                    linkOther = linkToLeft;

                    if (true == SetLink(keyNext, triangleNext, linkNext, out int keyNextNext, out int not_used))
                    {
                        y = ((keyNextNext >> 8) & 0xFF) - ((key >> 8) & 0xFF);
                        SetTile(key, linkToRight, y);
                    }
                }

                //same tile
                Tile_t tileNext = map[keyNext];
                int index0, index1;

                switch (linkMy)
                {
                    case 1: index0 = 9; index1 = 7; break;
                    case 2: index0 = 15; index1 = 9; break;
                    case 4: index0 = 2; index1 = 8; break;
                    case 5: index0 = 8; index1 = 2; break;
                    case 7: index0 = 7; index1 = 1; break;
                    case 8: index0 = 1; index1 = 7; break;
                    case 10: index0 = 12; index1 = 6; break;
                    case 11: index0 = 6; index1 = 12; break;
                    default: continue;
                }

                if (false == tileNext.IsMovable(index0)
                    || false == tileNext.IsMovable(index1))
                {
                    continue;
                }

                y = ((keyNext >> 8) & 0xFF) - ((key >> 8) & 0xFF);
                SetTile(key, linkOther, y);
            }

            DebugLog(key);
        }

        //// save data
        DataTable.WriteBinaryMappingData<Tile_t>(map, transformRsc.GetChild(0).gameObject.name);
    }

    public static void InitTile(Transform transform, Mesh mesh, float scale, byte layer, byte status)
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

    // get

    private static long GetHeightFlag(Vector3 diff, float size_quater, float size_quater_inverse)
    {
        diff = PTile.SnappingPoint(diff, size_quater, 3);
        int x = (int)(diff.x * size_quater_inverse);
        long y = (long)(diff.y * size_quater_inverse);  //y: 0 ~ 4 (0b000 ~ 0b100)
        int z = (int)(diff.z * size_quater_inverse);

        int shift;
        switch (x * 10 + z)
        {
            case 00: shift = 0; break;
            case 20: shift = 1; break;
            case 40: shift = 2; break;
            case 02: shift = 3; break;
            case 22: shift = 4; break;
            case 42: shift = 5; break;
            case 04: shift = 6; break;
            case 24: shift = 7; break;
            case 44: shift = 8; break;
            case 11: shift = 9; break;
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
    private int GetLinkIndex(int triangle)
    {
        switch (triangle)
        {
            case 0: return 1;
            case 3: return 11;
            case 4: return 2;
            case 5: return 4;
            case 10: return 8;
            case 11: return 10;
            case 13: return 5;
            case 14: return 7;
        }

        return -1;
    }
    private bool HasLinkedTile(int key, int triangleMy, out int keyNext, out int triangleNext, out int y)
    {
        y = int.MinValue;
        keyNext = int.MinValue;
        triangleNext = -1;
        switch (triangleMy)
        {
            case 0:
                keyNext = key + (0 << 16) - (1 << 0);
                triangleNext = 10;
                break;
            case 10:
                keyNext = key + (0 << 16) + (1 << 0);
                triangleNext = 0;
                break;

            case 4:
                keyNext = key + (0 << 16) - (1 << 0);
                triangleNext = 14;
                break;
            case 14:
                keyNext = key + (0 << 16) + (1 << 0);
                triangleNext = 4;
                break;

            case 3:
                keyNext = key - (1 << 16) + (0 << 0);
                triangleNext = 5;
                break;
            case 5:
                keyNext = key + (1 << 16) + (0 << 0);
                triangleNext = 3;
                break;

            case 11:
                keyNext = key - (1 << 16) + (0 << 0);
                triangleNext = 13;
                break;
            case 13:
                keyNext = key + (1 << 16) + (0 << 0);
                triangleNext = 11;
                break;

            default: return false;
        }

        for (y = -1; y <= 1; ++y)
        {
            if (false == map.TryGetValue(keyNext + y * (1 << 8), out Tile_t tileNext))
            {
                continue;
            }
            if (false == tileNext.IsMovable(triangleNext))
            {
                return false;
            }

            Tile_t tileMy = map[key];
            if (tileMy.GetTriangleHeightMask(triangleMy, -y * 4)
                    == tileNext.GetTriangleHeightMask(triangleNext, 0))
            {
                keyNext += y * (1 << 8);
                return true;
            }
        }

        return false;
    }

    // set
    private bool SetLink(int key, int triangle, int indexLink, out int keyNext, out int trinagleNext)
    {
        Tile_t tileMy = map[key];
        keyNext = -1;
        trinagleNext = -1;

        if (false == tileMy.IsMovable(triangle))
        {
            return false;
        }

        if (false == HasLinkedTile(key, triangle, out keyNext, out trinagleNext, out int y))
        {
            return false;
        }

        SetTile(key, indexLink, y);
        return true;
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

        float size_half = PTile.GetSize(TileSize.Half, scale);
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
            int move = PTile.GetMoveFlag(pointCenter - pivot, size);

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
                info |= tile.Info;
                move |= tile.Move;
                height |= tile.Height;
                map[key] = new Tile_t(info, move, height);
            }
        }
    }
    private void SetTile(int key, int indexLink, int y)
    {
        Tile_t tile = map[key];
        int info = tile.Info;

        if (0 != (info & (0b11 << (indexLink * 2))))
        {
            return;
        }

        switch (y)
        {
            case  0: info |= 0b01 << (indexLink * 2); break;
            case  1: info |= 0b10 << (indexLink * 2); break;
            case -1: info |= 0b11 << (indexLink * 2); break;
        }
        map[key] = new Tile_t(info, tile.Movement);

        //Debug.Log($"{PTile.GetPivot(key, tile.Scale)} {y} << {indexLink}*2");
    }

    // util
    private void DebugLog(int key)
    {
        Tile_t tile = map[key];

        string move = System.Convert.ToString(tile.Move, 2).ToString();
        string height = System.Convert.ToString(tile.Height, 2).ToString();
        string link = System.Convert.ToString(tile.Info & 0xFFFFFF, 2);
        Debug.Log($"{PTile.GetPivot(key, tile.Scale)}(scale:{tile.Scale}, status:{tile.Status}) m:{move} l:{link}\nh:{height}");
    }
}
#endif