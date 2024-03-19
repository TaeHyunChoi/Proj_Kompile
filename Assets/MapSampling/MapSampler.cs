using System.Collections.Generic;
using UnityEngine;
using CDataStructure;
using CMathf;
using static Public;

public class MapSampler : MonoBehaviour
{
    [SerializeField] private Transform resourceTransform;

    private Dictionary<int, Tile_t> map;
    private MeshFilter[] filter;

    private void Awake()
    {
        filter = resourceTransform.GetComponentsInChildren<MeshFilter>();
        map = new Dictionary<int, Tile_t>();
    }
    private void Start()
    {
        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            Mesh mesh = filter[f].mesh;

            Quaternion rot      = targetTransform.rotation;
            Vector3[] vertices  = mesh.vertices;
            Vector3[] normals   = mesh.normals;
            int[] triangles     = mesh.triangles;
            TileFeature character = TileFeature.None;
            switch (gameObject.tag)
            {
                case "Inner": character = TileFeature.Inner; break; //INNER
            }


            for (int t = 0; t < triangles.Length; t += 3)
            {
                int t0 = triangles[t];
                int t1 = triangles[t + 1];
                int t2 = triangles[t + 2];

                //Determine whether the mesh is the target for sampling by normal value.
                Vector3 normal1 = rot * normals[t0];
                Vector3 normal2 = rot * normals[t1];
                Vector3 normal3 = rot * normals[t2];
                Vector3 normal  = normal1;
                if (normal2.y < normal.y) { normal = normal2; }
                if (normal3.y < normal.y) { normal = normal3; }
                normal = CMath.FloorToVector(normal, 3);
                if (0 >= normal.y)
                {
                    continue;
                }

                //voxel_half is 0.25f, so you can use up to 2 decimal places.
                Vector3 A = CMath.FloorToVector(targetTransform.TransformPoint(vertices[t0]), 2);
                Vector3 B = CMath.FloorToVector(targetTransform.TransformPoint(vertices[t1]), 2);
                Vector3 C = CMath.FloorToVector(targetTransform.TransformPoint(vertices[t2]), 2);

                A = PVoxel.SnappingPoint(A, TILE_SIZE, 2);
                A = CMath.FloorToVector(A, 0);

                B = PVoxel.SnappingPoint(B, TILE_SIZE, 2);
                B = CMath.FloorToVector(B, 0);

                C = PVoxel.SnappingPoint(C, TILE_SIZE, 2);
                C = CMath.FloorToVector(C, 0);

                Set(A, B, C, character);
            }
        }

        //## When creating a link flag, use breadth-first search(BFS).
        int key = -1;
        Dictionary<int, int> searched = new Dictionary<int, int>();
        Queue<int> keys = new Queue<int>();
        foreach (int k in map.Keys)
        {
            key = k;
            break;
        }
        keys.Enqueue(key);

        while (keys.Count > 0)
        {
            int flag = 0;
            int targetKey = keys.Dequeue();

            if (true == searched.ContainsKey(targetKey))
            {
                continue;
            }
            else
            {
                searched.Add(targetKey, flag);
            }

            //relative coord: rx, ry, rz
            Tile_t targetVoxel = map[targetKey];
            for (int rx = -1; rx <= 1; ++rx)
            {
                for (int rz = -1; rz <= 1; ++rz)
                {
                    for (int ry = -1; ry <= 1; ++ry)
                    {
                        int neighborKey = targetKey + (rx * (1 << 16) + ry * (1 << 8) + rz);
                        if (false == map.TryGetValue(neighborKey, out Tile_t neighborVoxel))
                        {
                            continue;
                        }

                        bool bothMovable = false;
                        bool sameHeight = false;

                        int diffY = 0;
                        if      (ry ==  1) { diffY = +2; }
                        else if (ry == -1) { diffY = -2; }

                        switch (10 * rx + rz)
                        {
                            case -10 + 0: // (-1,  0)
                                bothMovable = targetVoxel.IsMovable(2) && neighborVoxel.IsMovable(0);
                                sameHeight  = targetVoxel.GetHeightCode(2) == neighborVoxel.GetHeightCode(1) + diffY;
                                sameHeight &= targetVoxel.GetHeightCode(3) == neighborVoxel.GetHeightCode(0) + diffY;
                                break;
                            case +10 + 0: // ( 1,  0)
                                bothMovable = targetVoxel.IsMovable(0) && neighborVoxel.IsMovable(2);
                                sameHeight  = targetVoxel.GetHeightCode(0) == neighborVoxel.GetHeightCode(3) + diffY;
                                sameHeight &= targetVoxel.GetHeightCode(1) == neighborVoxel.GetHeightCode(2) + diffY;
                                break;
                            case +00 + 1: // ( 0,  1)
                                bothMovable = targetVoxel.IsMovable(1) && neighborVoxel.IsMovable(3);
                                sameHeight  = targetVoxel.GetHeightCode(1) == neighborVoxel.GetHeightCode(0) + diffY;
                                sameHeight &= targetVoxel.GetHeightCode(2) == neighborVoxel.GetHeightCode(3) + diffY;
                                break;
                            case +00 - 1: // ( 0, -1)
                                bothMovable = targetVoxel.IsMovable(3) && neighborVoxel.IsMovable(1);
                                sameHeight  = targetVoxel.GetHeightCode(0) == neighborVoxel.GetHeightCode(1) + diffY;
                                sameHeight &= targetVoxel.GetHeightCode(3) == neighborVoxel.GetHeightCode(2) + diffY;
                                break;

                            case -10 + 1: // (-1,  1)
                                bothMovable  = targetVoxel.IsMovable(1) || targetVoxel.IsMovable(2);
                                bothMovable &= neighborVoxel.IsMovable(0) || neighborVoxel.IsMovable(3);
                                sameHeight   = targetVoxel.GetHeightCode(2) == neighborVoxel.GetHeightCode(0) + diffY;
                                break;
                            case -10 - 1: // (-1, -1)
                                bothMovable  = targetVoxel.IsMovable(2) || targetVoxel.IsMovable(3);
                                bothMovable &= neighborVoxel.IsMovable(0) || neighborVoxel.IsMovable(1);
                                sameHeight   = targetVoxel.GetHeightCode(1) == neighborVoxel.GetHeightCode(3) + diffY;
                                break;
                            case +10 + 1: // ( 1,  1)
                                bothMovable  = targetVoxel.IsMovable(0) || targetVoxel.IsMovable(1);
                                bothMovable &= neighborVoxel.IsMovable(2) || neighborVoxel.IsMovable(3);
                                sameHeight   = targetVoxel.GetHeightCode(1) == neighborVoxel.GetHeightCode(3) + diffY;
                                break;
                            case +10 - 1: // ( 1, -1)
                                bothMovable  = targetVoxel.IsMovable(0) || targetVoxel.IsMovable(1);
                                bothMovable &= neighborVoxel.IsMovable(2) || neighborVoxel.IsMovable(3);
                                sameHeight   = targetVoxel.GetHeightCode(1) == neighborVoxel.GetHeightCode(3) + diffY;
                                break;

                            case +00 + 0:
                                if (ry == 0)
                                {
                                    bothMovable = true;
                                    sameHeight = true;
                                }
                                break;
                        }

                        if (true == bothMovable && true == sameHeight)
                        {
                            int shift = (rx + 1) + 3 * (rz + 1);
                            if      (ry ==  1) { shift +=  9; }
                            else if (ry == -1) { shift += 18; }
                            flag |= 1 << shift;

                            if (false == searched.ContainsKey(neighborKey))
                            {
                                keys.Enqueue(neighborKey);
                            }
                        }
                    }
                }
            }

            map[targetKey] = new Tile_t(targetVoxel.DataFlag, flag);
        }

        //Save Mapping Data
        DataTable.WriteBinaryMappingData<Tile_t>(map, resourceTransform.GetChild(0).name);
        Debug.Log("Sampling done.");
    }
    private void Set(Vector3 p0, Vector3 p1, Vector3 p2, TileFeature feature)
    {
        Vector3 swap;
        float diagonal;

        //(Isosceles right triangle) Find the right angle point and store it in p0
        float v0to1 = CMath.Floor(Vector3.Distance(new Vector3(p0.x, 0, p0.z), new Vector3(p1.x, 0, p1.z)), 3);
        float v1to2 = CMath.Floor(Vector3.Distance(new Vector3(p1.x, 0, p1.z), new Vector3(p2.x, 0, p2.z)), 3);
        float v0to2 = CMath.Floor(Vector3.Distance(new Vector3(p0.x, 0, p0.z), new Vector3(p2.x, 0, p2.z)), 3);

        if (v0to1 == v1to2)
        {
            swap = p1;
            p1 = p0;
            p0 = swap;

            diagonal = v0to2;
        }
        else if(v1to2 == v0to2)
        {
            swap = p2;
            p2 = p0;
            p0 = swap;

            diagonal = v0to1;
        }
        else
        {
            //don`t need to swap. v0 is right angle point;
            diagonal = v1to2;
        }

        //Update and save information when the minimum unit (?) is reached
        if (TILE_SIZE < diagonal)
        {
            Vector3 midPoint = CMath.FloorToVector((p1 + p2) * 0.5f, 1);
            Set(p0, p1, midPoint, feature);
            Set(p0, p2, midPoint, feature);
        }
        else
        {
            //get point, get pivot
            Vector3 centroidPoint = CMath.FloorToVector((p0 + p1 + p2) * 0.33f, 2);
            centroidPoint = PVoxel.SnappingPoint(centroidPoint, TILE_HALF, 2);
            Vector3 pivot = PVoxel.GetPivot(centroidPoint);

            //set flag
            int movableFlag = PVoxel.SetMoveFlag(centroidPoint - pivot);
            int heightFlag = 0;
            heightFlag |= PVoxel.SetHeightFlag(p0 - pivot);
            heightFlag |= PVoxel.SetHeightFlag(p1 - pivot);
            heightFlag |= PVoxel.SetHeightFlag(p2 - pivot);

            //set voxel data
            int key = PVoxel.GetKey(centroidPoint);
            if (false == map.TryGetValue(key, out Tile_t voxel))
            {
                map.Add(key, new Tile_t(heightFlag | movableFlag));
            }
            else
            {
                map[key] = new Tile_t(voxel.DataFlag | heightFlag | movableFlag);
            }
        }
    }
}
