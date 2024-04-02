using System.Collections.Generic;
using UnityEngine;
using DevDataType;
using CMathf;
using System.Threading;

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
public class Dev_MapSampler9th : MonoBehaviour
{
    [SerializeField] 
    private Transform transformRsc;
    private static Dictionary<int, Tile_t> map;

    private void Awake()
    {
        map = new Dictionary<int, Tile_t>();
    }
    private void Start()
    {
        List<int> keys = new List<int>();
        foreach (int k in map.Keys)
        {
            keys.Add(k);
        }
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

            //tile_half is 0.5f, so you can use up to 2 decimal places.
            Vector3 A = CMath.FloorToVector(transform.TransformPoint(vertices[t0]), 2);
            Vector3 B = CMath.FloorToVector(transform.TransformPoint(vertices[t1]), 2);
            Vector3 C = CMath.FloorToVector(transform.TransformPoint(vertices[t2]), 2);

            float size = PTile.GetScale(TileSize.Default, scale);
            A = PTile.SnappingPoint(A, size, 2);
            B = PTile.SnappingPoint(B, size, 2);
            C = PTile.SnappingPoint(C, size, 2);

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

        float size_half = PTile.GetScale(TileSize.Half, scale);
        if (size_half < diagonal)
        {
            Vector3 midPoint = CMath.FloorToVector((p1 + p2) * 0.5f, 3);
            SetTileData(p0, p1, midPoint, scale, layer, info);
            SetTileData(p0, p2, midPoint, scale, layer, info);
        }
        else
        {
            float size = PTile.GetScale(TileSize.Default, scale);

            //get point, get pivot
            Vector3 pointCenter = PTile.SnappingPoint((p0 + p1 + p2) * 0.333f, size_half, 3);
            Vector3 pivot = PTile.GetPivot(pointCenter, size);

            //set flag
            int move = GetMoveFlag(pointCenter - pivot, size);

            int height = 0;
            height |= GetHeightFlag(p0 - pivot, size_half);
            height |= GetHeightFlag(p1 - pivot, size_half);
            height |= GetHeightFlag(p2 - pivot, size_half);

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
    private static int GetHeightFlag(Vector3 diff, float size)
    {
        //diff = CMath.FloorToVector(diff, 2);
        diff = PTile.SnappingPoint(diff, size, 2);
        if (diff.x % size != 0 || diff.z % size != 0)
        {
            return 0;
        }

        float size_inverse = 1 / size;
        int x = CMath.FloorToInt(diff.x * size_inverse, 2);
        int y = CMath.FloorToInt(diff.y * size_inverse * 2f, 2);
        int z = CMath.FloorToInt(diff.z * size_inverse, 2);

        //Debug.Log($"{diff:F3} {y} << ({x} + {z}*3)*3");
        return y << (x + z * 3) * 3;
    }
}
#endif