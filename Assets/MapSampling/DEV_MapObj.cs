using System.Collections.Generic;
using UnityEngine;
using DevDataType;
using CMathf;
using Unity.Collections;

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
public class DEV_MapObj : MonoBehaviour
{
    [SerializeField] 
    private TileFeature status;
    [SerializeField]
    private byte layer;

    //Dictionary<int, Tile_t> map과 Tile.cs를 연결하는 key값 => Mesh on/off에 사용
    //Field.cs에서 일정 간격만큼 Dicionary<int, Tile.cs>를 들고 있어야 하나?
    //Dev_tile과 Tile은 다르다 => Tile.cs가 Awake() 할 때에 map에다가 Mesh 넘기면 될 듯? (아니면 걍 transform.getChildren 하던가?
    private Mesh  mesh;
    private float scale;
    private Tile_sample[] tiles;
    private byte index;

    private void Awake()
    {
        mesh = transform.GetComponent<MeshFilter>().mesh;
        scale = (0 != ((byte)TileFeature.Small & (byte)status)) ? 0.5f : 1f;

        byte length = (byte)(1 / scale);
        tiles = new Tile_sample[length];
        for (int i = 0; i < tiles.Length; ++i)
        {
            tiles[i] = new Tile_sample(-1);
        }
        index = 0;

        int info = (layer << 6) | (int)status;
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

            SetTile(A, B, C, info);
        }
    }
    private void SetTile(Vector3 p0, Vector3 p1, Vector3 p2, int info)
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
            SetTile(p0, p1, midPoint, info);
            SetTile(p0, p2, midPoint, info);
        }
        else
        {
            float size = PTile.GetScale(TileSize.Default, scale);

            //get point, get pivot
            Vector3 pointCenter = CMath.FloorToVector((p0 + p1 + p2) * 0.333f, 3);
            Vector3 pivot       = PTile.GetPivot(pointCenter, size);

            //set flag
            int move = PTile.GetMoveFlag(pointCenter - pivot, size);

            int height = 0;
            height |= PTile.GetHeightFlag(p0 - pivot, size_half);
            height |= PTile.GetHeightFlag(p1 - pivot, size_half);
            height |= PTile.GetHeightFlag(p2 - pivot, size_half);

            //set tile data
            int key = (layer << 24) | PTile.GetKey(pointCenter, size);
            if (false == TryGetTile(key, out int indexTarget))
            {
                tiles[index++] = new Tile_sample(key, info, move, height);
            }
            else
            {
                Tile_sample tile = tiles[indexTarget];
                info    |= tile.Info;
                move    |= tile.Move;
                height  |= tile.Height;

                tiles[indexTarget] = new Tile_sample(key, info, move, height);
            }
        }
    }

    private bool TryGetTile(int key, out int index)
    {
        index = -1;
        for (int i = 0; i < tiles.Length; ++i)
        {
            if (key == tiles[i].Key)
            {
                index = i;
                return true;
            }
        }

        return false;
    }
    public bool TryGetTileArray(out Tile_sample[] tileArray)
    {
        if (0 == index)
        {
            tileArray = null;
            return false;
        }

        tileArray = tiles;
        return true;
    }
}
#endif