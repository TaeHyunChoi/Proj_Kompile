using CMathf;
using CDataStructure;
using static Public;

using UnityEngine;
using System.Collections.Generic;

public class MapSampler5th : MonoBehaviour
{
    [Header("File")]
    [SerializeField] private string fileName;

    [Header("Voxels")]
    [SerializeField] private Transform resourceTransform;
    [SerializeField] private float samplingInterval;

    [Header("Gizmos")]
    [SerializeField] private float drawHeightLow;
    [SerializeField] private float drawHeightHigh;

    private Dictionary<int, Voxel_t> map;
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
                map = DataTable.LoadMappingData(fileName);
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
        Coroutiner.PlayCoroutine(SamplingVoxels());
    }
    private IEnumerator<int> SamplingVoxels()
    {
        map = new Dictionary<int, Voxel_t>();

        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            Quaternion rotation = targetTransform.rotation;

            Mesh mesh = filter[f].mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 normal1 = rotation * normals[triangles[t]];
                normal1.Normalize();
                Vector3 normal2 = rotation * normals[triangles[t + 1]];
                normal2.Normalize();
                Vector3 normal3 = rotation * normals[triangles[t + 2]];
                normal3.Normalize();

                if (!GetSlopeData(normal1, normal2, normal3, out int slopeData))
                {
                    continue;
                }

                Vector3 A = targetTransform.TransformPoint(vertices[triangles[t]]);
                Vector3 B = targetTransform.TransformPoint(vertices[triangles[t + 1]]);
                Vector3 C = targetTransform.TransformPoint(vertices[triangles[t + 2]]);

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
                        SetVoxel(slopeData, samplingPoint);
                    }
                }
            }

            //Debug.Log($"Now Sampling ({f + 1}/{filter.Length})");
            yield return f + 1;
        }

        Debug.Log($"Sampling count:({map.Keys.Count})");
    }

    private bool GetSlopeData(Vector3 normal1, Vector3 normal2, Vector3 normal3, out int slopeData)
    {
        slopeData = 0;

        //Find the normal value that serves as the standard.
        //To avoid floating point problems, round off at 3 decimal places.
        Vector3 normal = normal1;
        if (normal2.y < normal.y) { normal = normal2; }
        if (normal3.y < normal.y) { normal = normal3; }
        normal = CMath.Floor1000Vector3(normal);

        //Set Slope Degree Type
        //There are only 4 slope angles.
        int shift = 4;
        if      (normal.y == -1.000f) { slopeData = OBSTACLE << shift; }
        else if (normal.y ==  1.000f) { slopeData = PLAIN    << shift; }
        else if (normal.y ==  0.500f) { slopeData = SLOPE30  << shift; }
        else if (normal.y ==  0.707f) { slopeData = SLOPE45  << shift; }
        else    { return false; }

        //Set Slope Diretion Flag (zero 0b_00, positive 0b_01, negative 0b_11)
        //The direction opposite to the direction indicated by normal value is the direction in which the slope increases.
        if (slopeData > (PLAIN << shift))
        {
            if      (normal.x < 0) { slopeData |= 0b_01_00; }
            else if (normal.x > 0) { slopeData |= 0b_11_00; }
            if      (normal.z < 0) { slopeData |= 0b_00_01; }
            else if (normal.z > 0) { slopeData |= 0b_00_11; }
        }

        return true;
    }
    private void SetVoxel(int slopeData, Vector3 point)
    {
        //Clamp(point) => get pivot => get index(key)
        Vector3 pivot = Parser.GetVoxelPivot(point);
        int key = Parser.GetVoxelKeyFromPivot(pivot);

        //Set Sub-voxel Type
        //1. Diagonal lines do not enter the point (sampling rule)
        //2. When moving, diagonal lines must be processed separately (all adjacent sub-voxels must be checked)
        int idxMove = Parser.GetSubVoxelIndex(pivot, point);
        Debug.Assert(idxMove != -1, "Wrong sub index");

        int sub      = slopeData >> 4;
        int slopeDir = slopeData & 0b_11_11;

        if (!map.TryGetValue(key, out Voxel_t voxel))
        {
            map.Add(key, new Voxel_t((slopeDir << SHIFT_SLOPE_DIRECTION) | (sub << idxMove * 2)));
            return;
        }

        //Update Sub voxel (ex. obstacle => slope)
        int targetSub = voxel.GetSubType(idxMove);
        if (sub > targetSub
            || (sub == OBSTACLE && targetSub == PLAIN)) //Exception: To treat the default value as obstacle
        {
            int newData = voxel.Data;
            newData &= ~(0b_11_11 << SHIFT_SLOPE_DIRECTION);
            newData |=   slopeDir << SHIFT_SLOPE_DIRECTION;

            newData &= ~(0b_11    << (idxMove * 2));
            newData |= sub << (idxMove * 2);

            map[key] = new Voxel_t(newData);
        }
    }
    private void OnDrawGizmos()
    {
        if (map == null)
        {
            return;
        }

        foreach (int key in map.Keys)
        {
            float x = (key & 0x_FF_0000) >> 16;
            float y = (key & 0x_00_FF00) >> 8;
            float z = (key & 0x_00_00FF);

            Vector3 pivot = new Vector3(x, y, z) * VOXEL_SIZE;

            if (pivot.y < drawHeightLow || pivot.y > drawHeightHigh)
            {
                continue;
            }

            //draw outline
            Gizmos.color = Color.grey;
            Gizmos.DrawLine(pivot, pivot + Vector3.forward * VOXEL_SIZE);
            Gizmos.DrawLine(pivot, pivot + Vector3.right * VOXEL_SIZE);
            Gizmos.DrawLine(pivot + Vector3.forward * VOXEL_SIZE, pivot + new Vector3(1,0,1) * VOXEL_SIZE);
            Gizmos.DrawLine(pivot + Vector3.right * VOXEL_SIZE, pivot + new Vector3(1, 0, 1) * VOXEL_SIZE);

            //draw sub-voxel type
            Voxel_t voxel = map[key];
            int move = voxel.SUB;
            Vector3 center = pivot + new Vector3(1, 0, 1) * VOXEL_HALF_SIZE;
            for (int i = 0; i < 4; ++i)
            {
                int masked = move & (0b11 << i*2);
                masked >>= i*2;

                if      (masked == PLAIN)    { Gizmos.color = Color.green; }
                else if (masked == OBSTACLE) { Gizmos.color = Color.red; }
                else if (masked == SLOPE30)  { Gizmos.color = Color.blue; }
                else if (masked == SLOPE45)  { Gizmos.color = Color.blue; }
                else { continue; }

                switch (i)
                {
                    case 0: Gizmos.DrawLine(center, center + Vector3.right   * VOXEL_HALF_SIZE); break;
                    case 1: Gizmos.DrawLine(center, center + Vector3.forward * VOXEL_HALF_SIZE); break;
                    case 2: Gizmos.DrawLine(center, center + Vector3.left    * VOXEL_HALF_SIZE); break;
                    case 3: Gizmos.DrawLine(center, center + Vector3.back    * VOXEL_HALF_SIZE); break;
                }
            }
        }
    }
}