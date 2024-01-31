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
        VoxelType type;
        Vector3 A, B, C;
        Vector3 elipson = Vector3.one * -0.0001f;

        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            Mesh mesh = filter[f].mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;
            float offset;

            for (int t = 0; t < triangles.Length; t += 3)
            {
                // get status
                Quaternion rot = targetTransform.rotation;
                Vector3 normal1 = rot * normals[triangles[t]];
                        normal1.Normalize();
                Vector3 normal2 = rot * normals[triangles[t + 1]];
                        normal2.Normalize();
                Vector3 normal3 = rot * normals[triangles[t + 2]];
                        normal3.Normalize();
                type = GetVoxelType(normal1.y, normal2.y, normal3.y);

                A = targetTransform.TransformPoint(vertices[triangles[t]]);
                B = targetTransform.TransformPoint(vertices[triangles[t + 1]]);
                C = targetTransform.TransformPoint(vertices[triangles[t + 2]]);

                #region 주석 처리
                /*
if (type == VoxelType.Obstacle
    && !targetTransform.gameObject.CompareTag("Slope"))
{
    int ra = Parser.GetVoxelRadix(Parser.GetCenterPoint(A));
    int rb = Parser.GetVoxelRadix(Parser.GetCenterPoint(B));
    int rc = Parser.GetVoxelRadix(Parser.GetCenterPoint(C));

    if (!data.ContainsKey(ra)) { data.Add(ra, new Voxel_t(0x0000)); }
    if (!data.ContainsKey(rb)) { data.Add(rb, new Voxel_t(0x0000)); }
    if (!data.ContainsKey(rc)) { data.Add(rc, new Voxel_t(0x0000)); }

    for (int i = 0; i < 4; ++i)
    {
        int typeBits = (int)type << (i * 2);

        if (type > data[ra].GetSubType(i))
        {
            int sub = data[ra].SubVoxel;
            int mask = 0b11 << (i * 2);

            sub &= ~mask;
            sub |= typeBits;
            data[ra] = new Voxel_t(sub);
        }

        if (type > data[rb].GetSubType(i))
        {
            int sub = data[rb].SubVoxel;
            int mask = 0b11 << (i * 2);

            sub &= ~mask;
            sub |= typeBits;
            data[rb] = new Voxel_t(sub);
        }

        if (type > data[rc].GetSubType(i))
        {
            int sub = data[rc].SubVoxel;
            int mask = 0b11 << (i * 2);

            sub &= ~mask;
            sub |= typeBits;
            data[rc] = new Voxel_t(sub);
        }
    }
}
//*/
                #endregion

                float distAB = Vector3.Distance(A, B);
                float interval = (grid > distAB) ? samplingInterval * 4f : samplingInterval;
                int samplingCountAB = (int)(distAB / grid * interval);
                offset = grid * 0.25f;

                for (int i = 0; i < samplingCountAB; ++i)
                {
                    float ratio = (float)i / samplingCountAB;
                    Vector3 AB = Vector3.Lerp(A, B, ratio);
                    Vector3 AC = Vector3.Lerp(A, C, ratio);

                    float distABtoAC = Vector3.Distance(AB, AC);
                    int samplingCountABtoAC = (int)(distABtoAC / grid * interval);

                    for (int j = 0; j < samplingCountABtoAC; ++j) 
                    {
                        Vector3 samplingPoint = Vector3.Lerp(AB, AC, (float)j / samplingCountABtoAC);
                        SetVoxel(samplingPoint, type);
                        SetVoxel(samplingPoint + normal1 * offset, type);
                        SetVoxel(samplingPoint + normal2 * offset, type);
                        SetVoxel(samplingPoint + normal3 * offset, type);
                    }
                }
            }
        }

        resourceTransform.gameObject.SetActive(false);
        Debug.Log($"Sampling Done. (count:{data.Keys.Count})");
        canDraw = true;
    }
    private VoxelType GetVoxelType(float n1, float n2, float n3)
    {
        bool plain = (n1 == 1 && n2 == 1 && n3 == 1);

        bool obstacle = n1 <= float.Epsilon
                        || n2 <= float.Epsilon
                        || n3 <= float.Epsilon;

        bool slope = Mathf.Approximately(Mathf.Acos(n1) * Mathf.Rad2Deg, 45)
                    || Mathf.Approximately(Mathf.Acos(n2) * Mathf.Rad2Deg, 45)
                    || Mathf.Approximately(Mathf.Acos(n3) * Mathf.Rad2Deg, 45);
                    
        VoxelType type = VoxelType.None;
        if (plain)    { type = VoxelType.Plain; }
        if (obstacle) { type = VoxelType.Obstacle; }
        if (slope)    { type = VoxelType.Slope; }

        return type;
    }
    private int GetBitShift(Vector3 diff)
    {
        int shift;

        float angle = Mathf.Atan2(diff.z, diff.x) * Mathf.Rad2Deg;
        angle = (angle + 360) % 360;

        if (diff.y <= 0)
        {
            if      (angle >= 0 && angle < 90)    { shift = 0; }
            else if (angle >= 90 && angle < 180)  { shift = 1; }
            else if (angle >= 180 && angle < 270) { shift = 2; }
            else                                  { shift = 3; }
        }
        else
        {
            if      (angle >= 0 && angle < 90)    { shift = 4; }
            else if (angle >= 90 && angle < 180)  { shift = 5; }
            else if (angle >= 180 && angle < 270) { shift = 6; }
            else                                  { shift = 7; }
        }

        return shift;
    }
    private void SetVoxel(Vector3 point, VoxelType type)
    {
        Vector3 center = Parser.GetCenterPoint(point);
        int radix = Parser.GetVoxelRadix(center);

        Vector3 diff = point - center;
        int quarant = GetBitShift(diff);
        int typeBits = (int)type << (quarant * 2);

        if (!data.ContainsKey(radix))
        {
            data.Add(radix, new Voxel_t(typeBits));
        }

        if (type > data[radix].GetSubType(quarant))
        {
            int sub = data[radix].SubVoxel;
            int mask = 0b11 << (quarant * 2);

            sub &= ~mask;
            sub |= typeBits;
            data[radix] = new Voxel_t(sub);
        }
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

                if (center.y < grid * 1.25f)
                    continue;

                bool IsContoured = false;
                for (int i = 0; i < 4; ++i)
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
                        default: continue;
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