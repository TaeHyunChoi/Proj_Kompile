using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Public;

public class MapSampler2nd : MonoBehaviour
{
    [Header("File")]
    [SerializeField] private string fileName;

    [Header("Voxels")]
    [SerializeField] private Transform resourceTransform;
    [SerializeField] private float samplingInterval;
    [SerializeField] private Mesh gizmoMesh;

    private Dictionary<int, Voxel_t> data;
    private MeshFilter[] filter;

    [Header("Grids")]
    [SerializeField] private float[] alpha;

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
            if (data != null)
            {
                data.Clear();
            }
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            data = DataTable.LoadMapVoxel(fileName);
            SamplingVoxels();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (data != null)
            {
                data.Clear();
            }

            filter = resourceTransform.GetComponentsInChildren<MeshFilter>();
            SamplingVoxels();
        }
    }

    private void SamplingVoxels()
    {
        data = new Dictionary<int, Voxel_t>();

        VoxelType type;
        Vector3 A, B, C;

        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            Mesh mesh = filter[f].mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;
            Quaternion rot = targetTransform.rotation;

            for (int t = 0; t < triangles.Length; t += 3)
            {
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

                Debug.Log($"{normal1:F2}, {normal2:F2}, {normal3:F2}");

                float distAB = Vector3.Distance(A, B);
                float interval = (GRID_SIZE > distAB) ? samplingInterval * 4f : samplingInterval;
                int samplingCountAB = (int)(distAB / GRID_SIZE * interval);

                for (int i = 0; i < samplingCountAB; ++i)
                {
                    float ratio = (float)i / samplingCountAB;
                    Vector3 AB = Vector3.Lerp(A, B, ratio);
                    Vector3 AC = Vector3.Lerp(A, C, ratio);

                    float distABtoAC = Vector3.Distance(AB, AC);
                    int samplingCountABtoAC = (int)(distABtoAC / GRID_SIZE * interval);

                    for (int j = 0; j < samplingCountABtoAC; ++j)
                    {
                        Vector3 samplingPoint = Vector3.Lerp(AB, AC, (float)j / samplingCountABtoAC);

                        //부동소수점 > *1000하여 판단 (소수점 3자리까지만)
                        int x = Mathf.CeilToInt(samplingPoint.x * 1000f);
                        int y = Mathf.CeilToInt(samplingPoint.y * 1000f);
                        int z = Mathf.CeilToInt(samplingPoint.z * 1000f);
                        SetVoxel(x, y, z, type);
                    }
                }
            }
        }
    }
    private VoxelType GetVoxelType(float n1, float n2, float n3)
    {
        bool isPlain = (n1 == 1 && n2 == 1 && n3 == 1);

        bool isObstacle = n1 <= float.Epsilon
                        || n2 <= float.Epsilon
                        || n3 <= float.Epsilon;

        bool isSlope = Mathf.Approximately(Mathf.Acos(n1) * Mathf.Rad2Deg, 45)
                    || Mathf.Approximately(Mathf.Acos(n2) * Mathf.Rad2Deg, 45)
                    || Mathf.Approximately(Mathf.Acos(n3) * Mathf.Rad2Deg, 45);

        VoxelType type = VoxelType.None;
        if (isObstacle) { type = VoxelType.Obstacle; }
        if (isSlope)    { type = VoxelType.Slope; }
        if (isPlain)    { type = VoxelType.Plain; }

        return type;
    }
    private void SetVoxel(int x1000, int y1000, int z1000, VoxelType type)
    {
        Vector3 point1000 = new Vector3(x1000, y1000, z1000);
        Vector3 center = GetCenterPoint(point1000);

        int radix = Parser.GetVoxelRadix(center);
        if (!data.ContainsKey(radix))
        {
            data.Add(radix, new Voxel_t(0x0000));
        }

        int[] quarant = GetBitShift((point1000 * 0.001f) - center);
        for (int i = 0; i < quarant.Length; ++i)
        {
            int typeBits = (int)type << (quarant[i] * 2);
            if (type > data[radix].GetSubType(quarant[i]))
            {
                int sub = data[radix].SubVoxel;
                int mask = 0b11 << (quarant[i] * 2);

                sub &= ~mask;
                sub |= typeBits;
                data[radix] = new Voxel_t(sub);
            }
        }
    }
    private int[] GetBitShift(Vector3 diff)
    {
        List<int> shift = new List<int>();

        float angle = Mathf.Atan2(diff.z, diff.x) * Mathf.Rad2Deg;
        angle = (angle + 360) % 360;

        if (angle == 360 || angle == 0) { shift.Add(0); shift.Add(3); }
        if (0 < angle && angle < 90)    { shift.Add(0); }

        if (angle == 90)                { shift.Add(0); shift.Add(1); }
        if (90 < angle && angle < 180)  { shift.Add(1); }

        if (angle == 180)               { shift.Add(1); shift.Add(2); }
        if (180 < angle && angle < 270) { shift.Add(2); }

        if (angle == 270)               { shift.Add(2); shift.Add(3); }
        if (270 < angle && angle < 360)  { shift.Add(3); }
        

        if (diff.y > 0)
        {
            for (int i = 0; i < shift.Count; ++i)
            {
                shift[i] += 4;
            }
        }

        return shift.ToArray();
    }
    public static Vector3 GetCenterPoint(Vector3 point)
    {
        float grid = GRID_SIZE * 1000f;
        float grid_invert = 1 / grid;

        float half_grid = grid * 0.5f;
        float half_grid_invert = 1 / half_grid;

        float cx = Mathf.Floor(point.x * half_grid_invert) * half_grid;
        float cy = Mathf.Floor(point.y * grid_invert) * 2f * half_grid + (1 * half_grid);
        float cz = Mathf.Floor(point.z * half_grid_invert) * half_grid;

        Vector3 center;
        Vector3 p1, p2;

        if ((cz - cx) % grid == 0)
        {
            p1 = new Vector3(cx, cy, cz);                                    // half-size clamp
            p2 = new Vector3(cx + half_grid, cy, cz + half_grid);  // half-size clamp + new Vector3(1,0,1);
        }
        else
        {
            p1 = new Vector3(cx + half_grid, cy, cz);     // half-size clamp + Vector3.right
            p2 = new Vector3(cx, cy, cz + half_grid);     // half-size clamp + Vector3.up
        }

        if (Vector3.Distance(point, p1) <= Vector3.Distance(point, p2))
        {
            center = p1;
        }
        else
        {
            center = p2;
        }

        cx = (center.x < 0f) ? 0f : center.x;
        cy = (center.y < 0f) ? half_grid : center.y;
        cz = (center.z < 0f) ? 0f : center.z;

        return new Vector3(cx, cy, cz) * 0.001f;
    }

    private void OnDrawGizmos()
    {
        if (data == null)
        {
            return;
        }

        float halfGrid = GRID_SIZE * 0.5f;
        float quaterGrid = GRID_SIZE * 0.25f;

        //draw voxel
        foreach (int radix in data.Keys)
        {
            float x = radix >> 16;
            float y = (radix & 0xFF00) >> 8;
            float z = radix & 0xFF;

            Vector3 center = new Vector3(x, y, z) * halfGrid;

            //if (center.y > GRID_SIZE)
            //    continue;

            bool IsContoured = false;
            for (int i = 0; i < 8; ++i)
            {
                int sub_type = (data[radix].SubVoxel & (0b11 << i * 2)) >> i * 2;
                switch ((VoxelType)sub_type)
                {
                    case VoxelType.Plain: Gizmos.color = new Color(0, 1, 0, alpha[0]); break;
                    case VoxelType.Slope: Gizmos.color = new Color(0, 0, 1, alpha[1]); break;
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
