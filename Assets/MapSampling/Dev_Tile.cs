using System.Collections.Generic;
using UnityEngine;
using CDataStructure;
using CMathf;

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
public class Dev_Tile : MonoBehaviour
{
    [SerializeField] 
    private TileFeature status;
    [SerializeField]
    private byte layer;
    private float scale;

    public float Size { get; set; }

    public void Set(Dictionary<int, Tile_t2> map)
    {
        int info = (layer << 6) | (int)status;
        if (0 != ((byte)TileFeature.Small & (byte)status))
        {
            scale = 0.5f;
        }
        else
        {
            scale = 1f;
        }


        Mesh mesh = transform.GetComponent<MeshFilter>().mesh;
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

            float size = PTile.GetSize(TileSize.Default, scale);
            Size = size;
            A = PTile.SnappingPoint(A, size, 2);
            B = PTile.SnappingPoint(B, size, 2);
            C = PTile.SnappingPoint(C, size, 2);

            SetTileData(map, A, B, C, info);
        }
    }
    private void SetTileData(Dictionary<int, Tile_t2> map, Vector3 p0, Vector3 p1, Vector3 p2, int info)
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
        if (size_half < diagonal)
        {
            Vector3 midPoint = CMath.FloorToVector((p1 + p2) * 0.5f, 3);
            SetTileData(map, p0, p1, midPoint, info);
            SetTileData(map, p0, p2, midPoint, info);
        }
        else
        {
            float size = PTile.GetSize(TileSize.Default, scale);

            //get point, get pivot
            Vector3 pointCenter = CMath.FloorToVector((p0 + p1 + p2) * 0.333f, 3);
            Vector3 pivot       = PTile.GetPivot(pointCenter, size);

            //set flag
            int move = PTile.GetMoveFlag(pointCenter - pivot, size);
            Debug.Assert(move != -1);

            int height = 0;
            height |= PTile.GetHeightFlag(p0 - pivot, size_half);
            height |= PTile.GetHeightFlag(p1 - pivot, size_half);
            height |= PTile.GetHeightFlag(p2 - pivot, size_half);

            //set voxel data
            int key = PTile.GetKey(pointCenter, size);
            if (false == map.TryGetValue(key, out Tile_t2 tile))
            {
                map.Add(key, new Tile_t2(info, move, height));
            }
            else
            {
                info    |= tile.Info;
                move    |= tile.Move;
                height  |= tile.Height;

                map[key] = new Tile_t2(info, move, height);
            }
        }
    }
}
#endif