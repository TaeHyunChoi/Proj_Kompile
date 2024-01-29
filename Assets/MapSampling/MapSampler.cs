using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEditor;
using UnityEngine;

public class MapSampler : MonoBehaviour
{
    [Header("File")]
    [SerializeField] private string fileName;

    [Header("Voxels")]
    [SerializeField] private Transform resourceTransform;
    [SerializeField] private float grid;
    [SerializeField] private float samplingInterval;
    [SerializeField] private Mesh gizmoMesh;

    [Header("Grids")]
    [SerializeField] private bool drawGrids;
    private bool canDraw;
    [SerializeField] private float[] alpha;

    private static MapSampler instance;
    public static Dictionary<int, Voxel_t> Data { get => instance.data; }
    private Dictionary<int, Voxel_t> data;
    private MeshFilter[] filter;

    private void Awake()
    {
        instance = this;
        data = new Dictionary<int, Voxel_t>();
        filter = resourceTransform.GetComponentsInChildren<MeshFilter>();
    }
    private void Start()
    {
        if (fileName == string.Empty)
        {
            GameObject obj = resourceTransform.GetChild(0).gameObject;
            fileName = obj.name;
        }

        SamplingVoxels();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DataTable.WriteBinaryMapVoxel(data, fileName);
            Debug.Log("save");
        }
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            data.Clear();
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            data = DataTable.LoadMapVoxel(fileName);
            SamplingVoxels();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            data.Clear();
            filter = resourceTransform.GetComponentsInChildren<MeshFilter>();
            SamplingVoxels();
        }
    }

    private void SamplingVoxels()
    {
        float weight = grid / samplingInterval;
        VoxelType type;
        Vector3 epsilon = Vector3.one * 0.00001f;
        Vector3 A, B, C;

        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            Mesh mesh = filter[f].mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            for (int t = 0; t < triangles.Length; t += 3)
            {
                // get status
                Quaternion rot = targetTransform.rotation;
                Vector3 normal1 = rot * normals[triangles[t]];
                Vector3 normal2 = rot * normals[triangles[t + 1]];
                Vector3 normal3 = rot * normals[triangles[t + 2]];
                type = GetVoxelType(normal1.y, normal2.y, normal3.y);

                A = targetTransform.TransformPoint(vertices[triangles[t]]) + epsilon;
                B = targetTransform.TransformPoint(vertices[triangles[t + 1]]) + epsilon;
                C = targetTransform.TransformPoint(vertices[triangles[t + 2]]) + epsilon;

                float distAB = Vector3.Distance(A, B);
                float multiple = (distAB < grid) ? weight * 0.25f : weight;
                int samplingCountAB = (int)(distAB / (grid * multiple));

                for (int i = 0; i < samplingCountAB; ++i)
                {
                    float   ratio  = (float)i / samplingCountAB;
                    Vector3 fromAB = Vector3.Lerp(A, B, ratio);
                    Vector3 toAC   = Vector3.Lerp(A, C, ratio);

                    float distABtoAC = Vector3.Distance(fromAB, toAC);
                    int samplingCountABtoAC = (int)(distABtoAC / (grid * multiple)); //얘 계산 왜 이러냐... 그냥 multiple만 써도 되는거 아닌가?

                    for (int j = 1; j < samplingCountABtoAC - 1; ++j) //변 위의 점은 포함x
                    {
                        Vector3 samplingPoint = Vector3.Lerp(fromAB, toAC, (float)j / samplingCountABtoAC);

                        //// Get Center Point -> Change to Radix
                        Vector3 center = Parser.GetCenterPoint(samplingPoint);
                        int radix = Parser.GetVoxelRadix(center);

                        if (!data.ContainsKey(radix))
                        {
                            data.Add(radix, new Voxel_t(0x0000));
                        }

                        Vector3 diff = samplingPoint - center;
                        float angle = Mathf.Atan2(diff.z, diff.x) * Mathf.Rad2Deg;
                        angle = (angle + 360) % 360;

                        int shift;
                        if (diff.y <= 0)
                        {
                            if      (angle >= 0 && angle < 90)       { shift = 0; }
                            else if (angle >=  90 && angle < 180)    { shift = 1; }
                            else if (angle >= 180 && angle < 270)    { shift = 2; }
                            else                                     { shift = 3; }
                        }
                        else
                        {
                            if      (angle >= 0 && angle < 90)       { shift = 4; }
                            else if (angle >=  90 && angle < 180)    { shift = 5; }
                            else if (angle >= 180 && angle < 270)    { shift = 6; }
                            else                                     { shift = 7; }
                        }
                        shift *= 2;

                        int typeBits = (int)type << shift;
                        int sub = data[radix].SubVoxel;
                        int mask = 0b11 << shift;

                        if (typeBits > (sub & mask))
                        {
                            sub = data[radix].SubVoxel & ~mask;
                            sub |= typeBits;
                            data[radix] = new Voxel_t(sub);
                        }

                        if(type == VoxelType.Obstacle)
                        {
                            Vector3 blockPoint;

                            // normal1 = targetTransform.TransformDirection(normal1) * grid * 0.5f;
                            blockPoint = samplingPoint + normal1;
                            Debug.Log($"[1] {blockPoint} = {samplingPoint} + {normal1}");

                            // normal2 = targetTransform.TransformDirection(normal2) * grid * 0.5f;
                            blockPoint = samplingPoint + normal2;
                            Debug.Log($"[2] {blockPoint} = {samplingPoint} + {normal2}");

                            // normal3 = targetTransform.TransformDirection(normal3) * grid * 0.5f;
                            blockPoint = samplingPoint + normal3;
                            Debug.Log($"[3] {blockPoint} = {samplingPoint} + {normal3}");

                            // radix = Parser.GetVoxelRadix(samplingPoint);

                            // if (!data.ContainsKey(radix))
                            // {
                            //     data.Add(radix, new Voxel_t((int)type << shift));
                            // }
                            // typeBits = (int)type << shift;
                            // sub = data[radix].SubVoxel;
                            // mask = 0b11 << shift;

                            // if (typeBits > (sub & mask))
                            // {
                            //     sub = data[radix].SubVoxel & ~mask;
                            //     sub |= typeBits;
                            //     data[radix] = new Voxel_t(sub);
                            // }
                        }
                    }
                }
            }
        }

        Debug.Log($"Sampling Done. (count:{data.Keys.Count})");
        canDraw = true;
    }
    private VoxelType GetVoxelType(float n1, float n2, float n3)
    {
        bool plain = n1 == 1 && n2 == 1 && n3 == 1;
        bool obstacle = n1 <= float.Epsilon || n2 <= float.Epsilon || n3 <= float.Epsilon;
        bool slope = Mathf.Approximately(Mathf.Acos(n1) * Mathf.Rad2Deg, 45);

        VoxelType type = VoxelType.None;
        if (plain)    { type = VoxelType.Plain; }
        if (obstacle) { type = VoxelType.Obstacle; }
        if (slope)    { type = VoxelType.Slope; }

        return type;
    }

    private void OnDrawGizmos()
    {
        if (!canDraw)
        {
            return;
        }

        if (drawGrids)
        {
            float halfGrid = grid * 0.5f;
            float quaterGrid = grid * 0.25f;

            //draw voxel
            foreach (int radix in data.Keys)
            {
                float x = radix >> 16;
                float y = (radix & 0xFF00) >> 8;
                float z = radix & 0xFF;

                Vector3 center = new Vector3(x, y, z) * halfGrid;
                //Gizmos.color = Color.black;
                //Gizmos.DrawCube(center, Vector3.one * 0.025f);

                bool IsContoured = false;
                for (int i = 0; i < 8; ++i)
                {
                    int sub_type = (data[radix].SubVoxel & (0b11 << i * 2)) >> i * 2;
                    switch ((VoxelType)sub_type)
                    {
                        case VoxelType.Plain:    Gizmos.color = new Color(0, 1, 0, alpha[0]); break;
                        case VoxelType.Slope:    Gizmos.color = new Color(0, 0, 1, alpha[1]); break;
                        case VoxelType.Obstacle: Gizmos.color = new Color(1, 0, 0, alpha[2]); break;
                        default: continue;
                    }

                    Quaternion rot = Quaternion.Euler(0, 90 * (1 - i), 90);
                    Vector3 c = (i < 4) ? center + Vector3.down * quaterGrid : center + Vector3.up * quaterGrid;

                    switch (i)
                    {
                        case 0:
                        case 4:
                            c += new Vector3(1, 0, 1) * quaterGrid;
                            break;
                        case 1:
                        case 5:
                            c += new Vector3(-1, 0, 1) * quaterGrid;
                            break;
                        case 2:
                        case 6:
                            c += new Vector3(-1, 0, -1) * quaterGrid;
                            break;
                        case 3:
                        case 7:
                            c += new Vector3(1, 0, -1) * quaterGrid;
                            break;
                    }

                    Gizmos.matrix = Matrix4x4.TRS(c, rot, new Vector3(quaterGrid, quaterGrid, quaterGrid));
                    Gizmos.DrawMesh(gizmoMesh, Vector3.zero, Quaternion.identity);
                    Gizmos.matrix = Matrix4x4.identity;

                    Gizmos.color = new Color(1f, 0.922f, 0.016f, 0.10f);

                    if (!IsContoured)
                    {
                        Gizmos.DrawLine(center + Vector3.right * halfGrid, center + Vector3.back * halfGrid);
                        Gizmos.DrawLine(center + Vector3.back * halfGrid, center + Vector3.left * halfGrid);
                        Gizmos.DrawLine(center + Vector3.left * halfGrid, center + Vector3.forward * halfGrid);
                        Gizmos.DrawLine(center + Vector3.forward * halfGrid, center + Vector3.right * halfGrid);

                        Gizmos.DrawLine(center + Vector3.left * halfGrid, center + Vector3.right * halfGrid);
                        Gizmos.DrawLine(center + Vector3.forward * halfGrid, center + Vector3.back * halfGrid);
                        IsContoured = true;
                    }
                }
            }
        }
    }
}