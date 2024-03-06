using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CDataStructure;
using CMathf;
using static Public;

public class MapSampling7th : MonoBehaviour
{
    [SerializeField] private Transform resourceTransform;

    private Dictionary<int, Voxel_t2> map;
    private MeshFilter[] filter;

    private void Awake()
    {
        filter = resourceTransform.GetComponentsInChildren<MeshFilter>();
        map = new Dictionary<int, Voxel_t2>();
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

            for (int t = 0; t < triangles.Length; t += 3)
            {
                int t0 = triangles[t];
                int t1 = triangles[t + 1];
                int t2 = triangles[t + 2];

                if (false == IsTargetPlane(rot, normals, t0, t1, t2))
                {
                    continue;
                }

                //voxel_half is 0.25f, so you can use up to 2 decimal places.
                Vector3 A = CMath.FloorToVector(targetTransform.TransformPoint(vertices[t0]), 2);
                Vector3 B = CMath.FloorToVector(targetTransform.TransformPoint(vertices[t1]), 2);
                Vector3 C = CMath.FloorToVector(targetTransform.TransformPoint(vertices[t2]), 2);
                Set(A, B, C);
            }
        }

        foreach (int key in map.Keys)
        {
            Debug.Log($"{PVoxel.GetPivot(key)}:: {System.Convert.ToString(map[key].HeightFlag >> VOXEL_BIT_HEIGHT, 2)} | {System.Convert.ToString(map[key].MoveFlag, 2)}");
        }
    }
    private bool IsTargetPlane(Quaternion rot, Vector3[] normals, int t0, int t1, int t2)
    { 
        Vector3 normal1 = rot * normals[t0];
        Vector3 normal2 = rot * normals[t1];
        Vector3 normal3 = rot * normals[t2];

        Vector3 normal = normal1;
        if (normal2.y < normal.y) { normal = normal2; }
        if (normal3.y < normal.y) { normal = normal3; }
        normal = CMath.FloorToVector(normal, 3);

        return 0 < normal.y;    
    }
    private void Set(Vector3 p0, Vector3 p1, Vector3 p2)
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
        if (VOXEL_SIZE < diagonal)
        {
            Vector3 midPoint = (p1 + p2) * 0.5f;
            Set(p0, p1, midPoint);
            Set(p0, p2, midPoint);
        }
        else
        {
            //get point, get pivot
            Vector3 centroidPoint = CMath.FloorToVector((p0 + p1 + p2) * 0.333f, 3);
            Vector3 pivot = PVoxel.GetPivot(centroidPoint);

            //set flag
            int movableFlag = PVoxel.GetMoveFlag(centroidPoint - pivot);
            int heightFlag = 0;
            heightFlag |= PVoxel.GetHeightFlag(p0 - pivot);
            heightFlag |= PVoxel.GetHeightFlag(p1 - pivot);
            heightFlag |= PVoxel.GetHeightFlag(p2 - pivot);

            //set voxel data
            int key = PVoxel.GetKeyFromPoint(centroidPoint);
            if (false == PVoxel.Get(map, centroidPoint, out Voxel_t2 voxel))
            {
                map.Add(key, new Voxel_t2(heightFlag | movableFlag));
            }
            else
            {
                map[key] = new Voxel_t2(voxel.Data | heightFlag | movableFlag);
            }
        }
    }

    #region Height calculation example code
    /*
    [SerializeField] private Vector2 point;
    [SerializeField] private GameObject obj;
    private void Start()
    {
        point = new Vector2(0.5f, 0.5f);
        obj.transform.position = new Vector3(point.x, 0f, point.y);
    }
    private void Update()
    {
        float x = point.x;
        float z = point.y;

        Vector3 pa = new Vector3(1, 0, 0);
        Vector3 pb = new Vector3(0, 0, 1);
        Vector3 pc = new Vector3(1, 1, 1);

        Vector3 ab = pb - pa;
        Vector3 ac = pc - pa;

        Vector3 normal = Vector3.Cross(ab, ac).normalized;

        float A = normal.x;
        float B = normal.y;
        float C = normal.z;
        float D = Vector3.Dot(normal, pa);

        float y = (-A * x + -C * z + D) / B;
        Debug.Log(y);
        obj.transform.position = new Vector3(x, y, z);
    }
     */
    #endregion
}
