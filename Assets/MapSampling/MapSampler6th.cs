using CDataStructure;
using CMathf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Public;

public class MapSampler6th : MonoBehaviour
{
    [Header("File")]
    [SerializeField] private string fileName;

    [Header("Voxels")]
    [SerializeField] private Transform resourceTransform;
    [SerializeField] private float samplingInterval;

    [Header("Gizmos")]
    [SerializeField] private Mesh gizmoMesh;
    [SerializeField] private float drawHeightLow;
    [SerializeField] private float drawHeightHigh;

    private Dictionary<int, Voxel_t2> map;
    private MeshFilter[] filter;

    private void Awake()
    {
        filter = resourceTransform.GetComponentsInChildren<MeshFilter>();
    }
    private void Start()
    {
        if (fileName == string.Empty)
        {
            GameObject obj = resourceTransform.GetChild(0).gameObject;
            fileName = obj.name;
        }

        Sampling();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DataTable.WriteBinaryMappingData(map, fileName);
            Debug.Log($"save: {fileName};");
        }
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (map != null)
            {
                map.Clear();
            }
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (map != null)
            {
                map = DataTable.LoadMappingData<Voxel_t2>(fileName);
                Debug.Log($"load: {fileName};");
            }
            else
            {
                Debug.Log("load: data is null;");
            }
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (map != null)
            {
                map.Clear();
            }
            filter = resourceTransform.GetComponentsInChildren<MeshFilter>();
            Sampling();
        }
    }

    private void Sampling()
    {
        IEnumerator sampling = Coroutiner.Play(SamplingVoxels());
        while (sampling.MoveNext())
        {
            //Debug.Log($"Progress: {CMath.Floor1000((float)sampling.Current) * 100f:F1} %");
        }
    }
    private IEnumerator<float> SamplingVoxels()
    {
        map = new Dictionary<int, Voxel_t2>();

        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            if (targetTransform.CompareTag("MapObject"))
            {
                continue;
            }

            Quaternion rotation = targetTransform.rotation;

            Mesh mesh = filter[f].mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            Hashtable table = new Hashtable();

            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 normal1 = rotation * normals[triangles[t]];
                normal1.Normalize();
                Vector3 normal2 = rotation * normals[triangles[t + 1]];
                normal2.Normalize();
                Vector3 normal3 = rotation * normals[triangles[t + 2]];
                normal3.Normalize();

                //The variable slopeFlag can have values ​​of -1, 0, 1, 2, 3.
                //There is special processing when slopeFlag == -1. (It means 'OBSTACLE')
                //The value 0xF0000 is a position that cannot be moved at all. So does not need to be saved.
                int heightFlag = GetHeightFlag(normal1, normal2, normal3);
                if (heightFlag == 0xF000)
                {
                    continue;
                }

                //Additional calculation of new Vector3(-0.001f, 0.001f, -0.001f) to reduce floating point issues
                Vector3 A = targetTransform.TransformPoint(vertices[triangles[t]])    /*  + new Vector3(-0.001f, 0.001f, -0.001f)*/;
                Vector3 B = targetTransform.TransformPoint(vertices[triangles[t + 1]])/*  + new Vector3(-0.001f, 0.001f, -0.001f)*/;
                Vector3 C = targetTransform.TransformPoint(vertices[triangles[t + 2]])/*  + new Vector3(-0.001f, 0.001f, -0.001f)*/;

                A = CMath.Floor1000Vector3(A);
                B = CMath.Floor1000Vector3(B);
                C = CMath.Floor1000Vector3(C);

                if (false == table.ContainsKey(A)) { table.Add(A, A.y); }
                if (false == table.ContainsKey(B)) { table.Add(B, B.y); }
                if (false == table.ContainsKey(C)) { table.Add(C, C.y); }

                continue;

                //테스트 좀 해봅시다.
                int index = TEST_GetTriIndex(normals, triangles[t], triangles[t + 1], triangles[t + 2], out Vector3 normal);
                Vector3 point = targetTransform.TransformPoint(vertices[triangles[index]]);
                float d = CMath.Floor1000((point.x * normal.x) + (point.y * normal.y) + (point.z * normal.z));
                Debug.Log($"point:{point:F3}, normal:{normal:F3}, d == {d:F3}\n\tvertices.y==({A.y:F3}, {B.y:F3}, {C.y:F3})");

                float distAB = Vector3.Distance(A, B);
                float interval = (VOXEL_SIZE > distAB) ? samplingInterval * 4f : samplingInterval;
                int samplingCountAB = CMath.FloorToInt1000(distAB / VOXEL_HALF_SIZE * interval);

                for (int i = 1; i < samplingCountAB - 1; ++i)
                {
                    float ratio = CMath.Floor1000((float)i / samplingCountAB);
                    Vector3 AB = Vector3.Lerp(A, B, ratio);
                    Vector3 AC = Vector3.Lerp(A, C, ratio);

                    float distABtoAC = Vector3.Distance(AB, AC);
                    int samplingCountABtoAC = CMath.FloorToInt1000(distABtoAC / VOXEL_HALF_SIZE * interval);

                    for (int j = 1; j < samplingCountABtoAC - 1; ++j)
                    {
                        ratio = CMath.Floor1000((float)j / samplingCountABtoAC);
                        Vector3 samplingPoint = CMath.Floor1000Vector3(Vector3.Lerp(AB, AC, ratio));

                        if (SetVoxel(heightFlag, samplingPoint))
                        {

                        }

                    }
                }
            }

            foreach (var point in table.Keys)
            {
                Debug.Log($"{point:F3}.y == {table[point]:F3}");
            }

            yield return (float)(f + 1) / filter.Length;
        }

        Debug.Log($"Sampling count:({map.Keys.Count})");
    }


    private int TEST_GetTriIndex(Vector3[] normals, int t1, int t2, int t3, out Vector3 normal)
    {
        int result = t1;

        normal = normals[t1];
        if (normals[t1].y < normal.y) { normal = normals[t1]; result = t1; }
        if (normals[t2].y < normal.y) { normal = normals[t2]; result = t2; }

        normal = CMath.Floor1000Vector3(normal);

        return result;
    }


    private int GetHeightFlag(Vector3 normal1, Vector3 normal2, Vector3 normal3)
    {
        //Find the normal value that serves as the standard.
        //To avoid floating point problems, round off at 3 decimal places.
        Vector3 normal = normal1;
        if (normal2.y < normal.y) { normal = normal2; }
        if (normal3.y < normal.y) { normal = normal3; }
        normal = CMath.Floor1000Vector3(normal);

        //Set Slope Degree Type
        //only 5 slope angles types. obstacle(can`t move) return -1;
        //variable 'h' means relative height in voxel vertices;
        int heightFlag = -1, h;

        switch (normal.y)
        {
            case  0.500f: h = 0b_01; break;   // slope30
            case  0.707f: h = 0b_10; break;   // slope45
            case  0.577f: h = 0b_10; break;   // slope45_partial

            case  1.000f: return  0;          // plain
            case -1.000f: return -1;  // obstacle, -1
            default:      return 0xF000;      // unnecessary data (ex. side)
        }

        //Set height flag(mapping)
        int dirFlag = 0;
        if      (normal.x < 0) { dirFlag |= 0b_01_00; }
        else if (normal.x > 0) { dirFlag |= 0b_11_00; }
        if      (normal.z < 0) { dirFlag |= 0b_00_01; }
        else if (normal.z > 0) { dirFlag |= 0b_00_11; }

        switch (dirFlag)
        {
            case 0b_01_00: heightFlag = (h << 6) | (0 << 4) | (0 << 2) | h; break;  // +x,  0
            case 0b_11_00: heightFlag = (0 << 6) | (h << 4) | (h << 2) | 0; break;  // -x,  0
            case 0b_00_01: heightFlag = (0 << 6) | (0 << 4) | (h << 2) | h; break;  //  0, +z
            case 0b_00_11: heightFlag = (h << 6) | (h << 4) | (0 << 2) | 0; break;  //  0, -z
            case 0b_01_01: heightFlag = (0 << 6) | (0 << 4) | (0 << 2) | h; break;  // +x, +z
            case 0b_01_11: heightFlag = (h << 6) | (0 << 4) | (0 << 2) | 0; break;  // +x, -z
            case 0b_11_01: heightFlag = (0 << 6) | (0 << 4) | (h << 2) | 0; break;  // -x, +z
            case 0b_11_11: heightFlag = (0 << 6) | (h << 4) | (0 << 2) | 0; break;  // -x, -z
        }

        return heightFlag;
    }
    private bool SetVoxel(int heightFlag, Vector3 point)
    {
        int voxelKey = PVoxel.GetKeyFromPoint(point);
        int moveFlag = 1 << PVoxel.GetMoveIndex(point);

        //Add voxel
        if (false == map.TryGetValue(voxelKey, out Voxel_t2 voxel))
        {
            map.Add(voxelKey, new Voxel_t2((heightFlag << 4) | moveFlag));
            return true;
        }

        //Update voxel data
        int newData = voxel.Data;
        int type = voxel.Type;

        //update case 01: input obstacle in PLAIN voxel;
        if (PLAIN == type && -1 == heightFlag)
        {
            newData = voxel.Data & ~moveFlag;

            //If this voxel can`t move, delete height flag also;
            if ((newData & 0x00F) == 0)
            {
                newData &= ~0xFF0; 
            }
        }

        //update case 01: input SLOPE in Any voxel;
        else if (0 != heightFlag)
        {
            //delete before height flag
            newData &= ~0xFF0;
            newData |= heightFlag;

            //add new (height | move) flag
            newData |= (heightFlag << 4) | moveFlag;
        }

        //else cases don`t need to update voxel data;
        else
        {
            return false;
        }

        //update
        map[voxelKey] = new Voxel_t2(newData);
        return true;
    }

    //private void OnDrawGizmos()
    //{
    //    if (null == map)
    //    {
    //        return;
    //    }
    //    foreach (int key in map.Keys)
    //    {
    //        //get pivot > decide whether to draw or not.
    //        Vector3 pivot = PVoxel.GetPivot(key);
    //        if (pivot.y < drawHeightLow || pivot.y > drawHeightHigh)
    //        {
    //            Gizmos.color = Color.clear;
    //            continue;
    //        }

    //        //get voxel data
    //        Voxel_t2 voxel = map[key];

    //        //draw outline
    //        Gizmos.color = Color.grey;
    //        Gizmos.DrawLine(pivot, pivot + Vector3.forward * VOXEL_SIZE);
    //        Gizmos.DrawLine(pivot, pivot + Vector3.right * VOXEL_SIZE);
    //        Gizmos.DrawLine(pivot + Vector3.forward * VOXEL_SIZE, pivot + new Vector3(1, 0, 1) * VOXEL_SIZE);
    //        Gizmos.DrawLine(pivot + Vector3.right * VOXEL_SIZE, pivot + new Vector3(1, 0, 1) * VOXEL_SIZE);

    //        Vector3 center = pivot + VOXEL_HALF_SIZE * new Vector3(1f, 0f, 1f);
    //        for (int i = 0; i < 4; ++i)
    //        {
    //            //한 번에 처리할 수도 있을 것 같은데
    //            if (true == voxel.CanMove(i))
    //            {
    //                Gizmos.color = Color.green;

    //                //이걸 어떻게 판별한담?
    //                if (true == voxel.HaveHeight(heightFlag: 1 << (i + VOXEL_BIT_HEIGHT)))
    //                {
    //                    Gizmos.color = Color.blue;
    //                }
    //            }
    //            else
    //            {
    //                Gizmos.color = Color.red;
    //            }

    //            switch (i)
    //            {
    //                case 0: Gizmos.DrawLine(center, center + Vector3.right * VOXEL_HALF_SIZE); break;
    //                case 1: Gizmos.DrawLine(center, center + Vector3.forward * VOXEL_HALF_SIZE); break;
    //                case 2: Gizmos.DrawLine(center, center + Vector3.left * VOXEL_HALF_SIZE); break;
    //                case 3: Gizmos.DrawLine(center, center + Vector3.back * VOXEL_HALF_SIZE); break;
    //            }
    //        }

    //    }
    //}
}
