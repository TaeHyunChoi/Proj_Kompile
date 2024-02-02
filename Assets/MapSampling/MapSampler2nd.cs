using System.Linq;
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

    [SerializeField] private float[] gizmoAlpha;

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

        SubVoxelType type;
        Vector3 A, B, C;

        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            Mesh mesh = filter[f].mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;
            Quaternion rot = targetTransform.rotation;

            //VoxelType objType;
            //if (targetTransform.CompareTag("Obstacle"))   { objType = VoxelType.Obstacle; }
            //else if (targetTransform.CompareTag("Slope")) { objType = VoxelType.Slope; }
            //else if (targetTransform.CompareTag("Plain")) { objType = VoxelType.Plain; }
            //else { objType = VoxelType.None; }

            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 normal1 = rot * normals[triangles[t]];
                normal1.Normalize();
                Vector3 normal2 = rot * normals[triangles[t + 1]];
                normal2.Normalize();
                Vector3 normal3 = rot * normals[triangles[t + 2]];
                normal3.Normalize();


                type = GetType(normal1.y, normal2.y, normal3.y);

                if (type == SubVoxelType.None)
                    continue;

                A = targetTransform.TransformPoint(vertices[triangles[t]]);
                B = targetTransform.TransformPoint(vertices[triangles[t + 1]]);
                C = targetTransform.TransformPoint(vertices[triangles[t + 2]]);

                float distAB = Vector3.Distance(A, B);
                float interval = (GRID_SIZE > distAB) ? samplingInterval * 4f : samplingInterval;
                int samplingCountAB = Mathf.FloorToInt(distAB / GRID_SIZE * interval);

                for (int i = 0; i < samplingCountAB; ++i)
                {
                    float ratio = (float)i / samplingCountAB;
                    Vector3 AB = Vector3.Lerp(A, B, ratio);
                    Vector3 AC = Vector3.Lerp(A, C, ratio);

                    float distABtoAC = Vector3.Distance(AB, AC);
                    int samplingCountABtoAC = Mathf.FloorToInt(distABtoAC / GRID_SIZE * interval);

                    int start = (type != SubVoxelType.Obstacle) ? 1 : 0;
                    for (int j = start; j < samplingCountABtoAC; ++j)
                    {
                        Vector3 samplingPoint = Vector3.Lerp(AB, AC, (float)j / samplingCountABtoAC);

                        //부동소수점 : *1000하여 판단 (소수점 3자리까지만)
                        int x = Mathf.CeilToInt(samplingPoint.x * 1000f);
                        int y = Mathf.CeilToInt(samplingPoint.y * 1000f);
                        int z = Mathf.CeilToInt(samplingPoint.z * 1000f);
                        Vector3 samplePoint1000 = new Vector3(x, y, z);

                        SetVoxel(samplePoint1000, type);

                        if (type == SubVoxelType.Obstacle)
                        {
                            for (int q = 0; q < 8; ++q)
                            {
                                Vector3 offset1000 = GetObstacleOffset(q);
                                SetVoxel(samplePoint1000 + offset1000, type, isForced: true);
                            }
                        }
                    }
                }
            }
        }

        Debug.Log($"Sampling Done. (count:{data.Keys.Count})");
    }
    private SubVoxelType GetType(float y1, float y2, float y3)
    {
        float[] y = new float[] { y1, y2, y3 };
        for (int i = 0; i < 3 - 1; ++i)
        {
            for (int j = 0; j < 3 - i - 1; ++j)
            {
                if (y[j] > y[j + 1])
                {
                    float temp = y[j];
                    y[j] = y[j + 1];
                    y[j + 1] = temp;
                }
            }
        }

        int min = Mathf.FloorToInt(y[0] * 1000f);

        SubVoxelType type;
        if (min == -1000) { type = SubVoxelType.Obstacle; }
        else if (min == 1000) { type = SubVoxelType.Plain; }
        else if (0 < min && min < 1000) { type = SubVoxelType.Slope45; }
        else { type = SubVoxelType.None; }

        //Debug.Log($"[{type}] {min} ({y[0]:F10})");
        return type;
    }

    private Vector3 GetObstacleOffset(int direction)
    {
        Vector3 dir = Vector3.zero;

        switch (direction)
        {
            case 0: dir = new Vector3( 1,  0,  1); break;
            case 1: dir = new Vector3(-1,  0,  1); break;
            case 2: dir = new Vector3(-1,  0, -1); break;
            case 3: dir = new Vector3( 1,  0, -1); break;
            case 4: dir = new Vector3( 1,  0,  0); break;
            case 5: dir = new Vector3(-1,  0,  0); break;
            case 6: dir = new Vector3( 0,  0, -1); break;
            case 7: dir = new Vector3( 0,  0,  1); break;
        }
        dir.Normalize();

        if (direction < 4)
        {
            dir *= GRID_SIZE * 0.25f * 1000f;
        }
        else
        {
            dir *= HALF_GRID_SIZE * 1000f;
        }

        int ox = Mathf.FloorToInt(dir.x);
        int oy = Mathf.FloorToInt(dir.y);
        int oz = Mathf.FloorToInt(dir.z);

        return new Vector3(ox, oy, oz);
    }
    private bool HaveNoneNeighbor(Vector3 centerPoint1000, int direction)
    {
        Vector3 dir1000 = Vector3.zero;
        switch (direction)
        {
            case 0: dir1000 = new Vector3(1, 0, 1); break;
            case 1: dir1000 = new Vector3(-1, 0, 1); break;
            case 2: dir1000 = new Vector3(-1, 0, -1); break;
            case 3: dir1000 = new Vector3(1, 0, -1); break;
            case 4: dir1000 = new Vector3(1, 0, 0); break;
            case 5: dir1000 = new Vector3(-1, 0, 0); break;
            case 6: dir1000 = new Vector3(0, 0, -1); break;
            case 7: dir1000 = new Vector3(0, 0, 1); break;
        }
        dir1000.Normalize();
        int ox = Mathf.FloorToInt(dir1000.x);
        int oy = Mathf.FloorToInt(dir1000.y);
        int oz = Mathf.FloorToInt(dir1000.z);
        dir1000 = new Vector3(ox, oy, oz);

        if (direction < 4)
        {
            dir1000 *= GRID_SIZE * 0.25f * 1000f;
        }
        else
        {
            dir1000 *= HALF_GRID_SIZE * 1000f;
        }

        Vector3 center = GetCenterPoint(centerPoint1000 + dir1000);
        int radix = Parser.GetVoxelRadix(center);

        if (!data.ContainsKey(radix)
            || data[radix].GetSubType(direction) != SubVoxelType.None)
        {
            return false;
        }

        return true;
    }

    private void SetVoxel(Vector3 point1000, SubVoxelType type, bool isForced = false)
    {
        Vector3 center = GetCenterPoint(point1000);
        int radix = Parser.GetVoxelRadix(center);
        if (!data.ContainsKey(radix))
        {
            data.Add(radix, new Voxel_t(0x0000));
        }

        int quarant = GetBitShift((point1000 * 0.001f) - center);
        if (quarant == -1)
        {
            return ;
        }

        int typeBits = (int)type << (quarant * 2);
        int sub = data[radix].SubVoxel;
        int mask = 0b11 << (quarant * 2);

        if (isForced
            || type > data[radix].GetSubType(quarant))
        {
            sub &= ~mask;
            sub |= typeBits;
            data[radix] = new Voxel_t(sub);
        }
        //else if (type > data[radix].GetSubType(quarant))
        //{
        //    sub &= ~mask;
        //    sub |= typeBits;
        //    data[radix] = new Voxel_t(sub);
        //}
    }
    private int GetBitShift(Vector3 diff)
    {
        float angle = Mathf.Atan2(diff.z, diff.x) * Mathf.Rad2Deg;
        angle = (angle + 360) % 360;

        int shift = -1;
        if (0 < angle && angle < 90) { shift = 0; }
        if (90 < angle && angle < 180) { shift = 1; }
        if (180 < angle && angle < 270) { shift = 2; }
        if (270 < angle && angle < 360) { shift = 3; }

        float dy = Mathf.FloorToInt(diff.y * 1000);
        if (dy > 1f)
        {
            shift += 4;
        }

        return shift;
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

            //if (center.y < GRID_SIZE)
            //    continue;

            bool IsContoured = false;
            for (int i = 0; i < 8; ++i)
            {
                int sub_type = (data[radix].SubVoxel & (0b11 << i * 2)) >> i * 2;
                switch ((SubVoxelType)sub_type)
                {
                    case SubVoxelType.Plain: Gizmos.color = new Color(0, 1, 0, gizmoAlpha[0]); break;
                    case SubVoxelType.Slope45: Gizmos.color = new Color(0, 0, 1, gizmoAlpha[1]); break;
                    case SubVoxelType.Obstacle: Gizmos.color = new Color(1, 0, 0, gizmoAlpha[2]); break;
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

                Gizmos.color = new Color(1f, 0.922f, 0.016f, 1f);

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
