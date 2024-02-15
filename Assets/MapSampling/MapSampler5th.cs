using CMathf;
using PublicValue;
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

    private Dictionary<int, Voxel_t5> map;
    private MeshFilter[] filter;

    private readonly int OBSTACLE = 0b_00;
    private readonly int PLAIN    = 0b_01;
    private readonly int SLOPE30  = 0b_10;
    private readonly int SLOPE45  = 0b_11;

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
            //DataTable.WriteBinaryMapVoxel(data, fileName);
            Debug.Log("save: Code not implemented;");
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
                //data = DataTable.LoadMapVoxel(fileName);
                Debug.Log("load: Code not implemented;");
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
        map = new Dictionary<int, Voxel_t5>();

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

                int data = SetSlopeData(normal1, normal2, normal3);
                if (data == -1)
                {
                    continue;
                }

                Vector3 A = targetTransform.TransformPoint(vertices[triangles[t]]);
                Vector3 B = targetTransform.TransformPoint(vertices[triangles[t + 1]]);
                Vector3 C = targetTransform.TransformPoint(vertices[triangles[t + 2]]);

                float distAB = Vector3.Distance(A, B);
                float interval = (VOXEL_SIZE > distAB) ? samplingInterval * 4f : samplingInterval;
                int samplingCountAB = CMath.FloorToInt1000(distAB / VOXEL_HALF_SIZE * interval);

                for (int i = 1; i < samplingCountAB; ++i)
                {
                    float ratio = CMath.Floor1000((float)i / samplingCountAB);
                    Vector3 AB = Vector3.Lerp(A, B, ratio);
                    Vector3 AC = Vector3.Lerp(A, C, ratio);

                    float distABtoAC = Vector3.Distance(AB, AC);
                    int samplingCountABtoAC = CMath.FloorToInt1000(distABtoAC / VOXEL_HALF_SIZE * interval);

                    for (int j = 1; j < samplingCountABtoAC; ++j)
                    {
                        ratio = CMath.Floor1000((float)j / samplingCountABtoAC);
                        Vector3 samplingPoint = CMath.Floor1000Vector3(Vector3.Lerp(AB, AC, ratio));
                        SetVoxel(data, samplingPoint);
                    }
                }
            }

            //Debug.Log($"Now Sampling ({f + 1}/{filter.Length})");
            yield return f + 1;
        }

        Debug.Log($"Sampling count:({map.Keys.Count})");
    }

    private int SetSlopeData(Vector3 normal1, Vector3 normal2, Vector3 normal3)
    {
        int data = 0, mask = 0;

        //Find the normal value that serves as the standard.
        //To avoid floating point problems, round off at 3 decimal places.
        Vector3 normal = normal1;
        if (normal2.y < normal.y) { normal = normal2; }
        if (normal3.y < normal.y) { normal = normal3; }
        normal = CMath.Floor1000Vector3(normal);

        //The side is excluded because it is not subject to movement.
        if (normal.y == 0.000f)
        {
            return -1;
        }

        //Set Slope Degree Type
        //There are only 4 slope angles.
        if      (normal.y == -1.000f) { mask = OBSTACLE; }
        else if (normal.y ==  1.000f) { mask = PLAIN; }
        else if (normal.y ==  0.707f) { mask = SLOPE45; }
        else if (normal.y ==  0.500f) { mask = SLOPE30; }
        data = mask << SHIFT_SLOPE_DEGREE;

        //Set Slope Diretion Flag (zero 0b_00, positive 0b_01, negative 0b_11)
        //The direction opposite to the direction indicated by normal value is the direction in which the slope increases.
        if (mask == SLOPE30 || mask == SLOPE45)
        {
            mask = 0b_00;
            if      (normal.x < 0) { mask |= 0b_01_00; }
            else if (normal.x > 0) { mask |= 0b_11_00; }
            if      (normal.z < 0) { mask |= 0b_00_01; }
            else if (normal.z > 0) { mask |= 0b_00_11; }
            data |= mask << SHIFT_SLOPE_DIRECTION;
        }

        return data;
    }
    private void SetVoxel(int data, Vector3 point)
    {
        //Clamp(point) => get pivot => get index(key)
        float cx = CMath.Floor1000(CMath.FloorToInt1000(point.x * VOXEL_INVERT) * VOXEL_SIZE);
        float cy = CMath.Floor1000(CMath.FloorToInt1000(point.y * VOXEL_INVERT) * VOXEL_SIZE);
        float cz = CMath.Floor1000(CMath.FloorToInt1000(point.z * VOXEL_INVERT) * VOXEL_SIZE);
        Vector3 pivot = new Vector3(cx, cy, cz);
        int key = Parser.GetVoxelIndex(pivot);

        //Set Sub-voxel Type
        //1. Diagonal lines do not enter the point (sampling rule)
        //2. When moving, diagonal lines must be processed separately (all adjacent sub-voxels must be checked)
        bool e1 = (point.z - pivot.z) >  (point.x - pivot.x);
        bool e2 = (point.z - pivot.z) > -(point.x - pivot.x) + VOXEL_SIZE;
        int idxMove = -1;
        if (!e1 &  e2) { idxMove = 0; }
        if ( e1 &  e2) { idxMove = 1; }
        if ( e1 & !e2) { idxMove = 2; }
        if (!e1 & !e2) { idxMove = 3; }
        Debug.Assert(idxMove != -1, "Wrong sub index");

        int type = data >> SHIFT_SLOPE_DEGREE;
        Debug.Assert(0 <= type && type < 4, $"Wrong sub type({type})");

        if (!map.TryGetValue(key, out Voxel_t5 voxel))
        {
            map.Add(key, new Voxel_t5(data));
        }

        else if (type > voxel.GetSubType(idxMove)
            || type == OBSTACLE && voxel.GetSubType(idxMove) == PLAIN) //Exception: To treat the default value as obstacle
        {
            int newData = voxel.Data;

            //Update Slope Degree (ex. obstacle => slope)
            int degree = data >> SHIFT_SLOPE_DEGREE;
            if (degree > voxel.SlopeDegree)
            {
                newData &= ~(0b11 << SHIFT_SLOPE_DEGREE);
                newData |= degree << SHIFT_SLOPE_DEGREE;
            }

            //Update Sub-voxel Type
            newData &= ~(0b11 << (idxMove * 2));
            newData |= type << (idxMove * 2);

            //Update Data
            map[key] = new Voxel_t5(newData);
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
            Voxel_t5 voxel = map[key];
            int move = voxel.Move;
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
