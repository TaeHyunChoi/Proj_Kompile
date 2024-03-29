using System.Collections.Generic;
using UnityEngine;
using CDataStructure;
using CMathf;
using static Public;

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
public class Dev_Tile : MonoBehaviour
{
    [SerializeField] 
    private TileFeature status;
    [SerializeField]
    private byte layer;

    private Tile_t2 tile;
    private int key;

    public Tile_t2 Tile { get => tile; }
    public int Key { get => key; }

    public void Set(Dictionary<int, Tile_t2> map)
    {
        byte info = (byte)((layer << 6) | (byte)status);


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

            A = PVoxel.SnappingPoint(A, TILE_SIZE, 2);
            B = PVoxel.SnappingPoint(B, TILE_SIZE, 2);
            C = PVoxel.SnappingPoint(C, TILE_SIZE, 2);

            SetTileData(map, A, B, C, info);
        }
    }
    private void SetTileData(Dictionary<int, Tile_t2> map, Vector3 p0, Vector3 p1, Vector3 p2, byte info)
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


        if (TILE_HALF * Mathf.Sqrt(2) < diagonal)
        {
            Vector3 midPoint = CMath.FloorToVector((p1 + p2) * 0.5f, 1);
            SetTileData(map, p0, p1, midPoint, info);
            SetTileData(map, p0, p2, midPoint, info);
        }
        else
        {
            //get point, get pivot
            Vector3 pointCenter = PVoxel.SnappingPoint((p0 + p1 + p2) * 0.33f, TILE_HALF, 2);
            Vector3 pivot       = PVoxel.GetPivot(pointCenter);

            //set flag
            int move = PVoxel.GetMoveFlag(pointCenter - pivot);
            Debug.Assert(move != -1);

            int height = PVoxel.GetHeightFlag(p0 - pivot, p1 - pivot, p2 - pivot);

            //set voxel data
            int key = PVoxel.GetKey(pointCenter);
            this.key = key;
            if (false == map.TryGetValue(key, out Tile_t2 tile))
            {
                this.tile = new Tile_t2(info, (byte)move, height);
                map.Add(key, this.tile);
            }
            else
            {
                info    |= tile.Info;
                move    |= tile.Move;
                height  |= tile.Height;

                this.tile = new Tile_t2(info, (byte)move, height);
                map[key] = this.tile;
            }
        }
    }
}
#endif