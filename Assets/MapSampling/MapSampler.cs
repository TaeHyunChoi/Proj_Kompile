using System.Collections.Generic;
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

            //법선으로 처리하면 tag가 많을 필요 없다.
            //if (targetTransform.CompareTag("Movable"))       { type = VoxelType.Plain; }
            //else if (targetTransform.CompareTag("Obstacle")) { type = VoxelType.Obstacle; }
            //else { type = VoxelType.None; }

            for (int t = 0; t < triangles.Length;)
            {
                // get status
                Vector3 normal1 = normals[triangles[t]];
                Vector3 normal2 = normals[triangles[t + 1]];
                Vector3 normal3 = normals[triangles[t + 2]];
                type = GetVoxelState(normal1, normal2, normal3);

                A = targetTransform.TransformPoint(vertices[triangles[t++]]) + epsilon;
                B = targetTransform.TransformPoint(vertices[triangles[t++]]) + epsilon;
                C = targetTransform.TransformPoint(vertices[triangles[t++]]) + epsilon;

                float distAB = Vector3.Distance(A, B);
                float multiple = (distAB < grid) ? weight * 0.25f : weight;
                int samplingCountAB = (int)(distAB / (grid * multiple));
                for (int i = 0; i < samplingCountAB; ++i)
                {
                    float ratio = (float)i / samplingCountAB;
                    Vector3 fromAB = Vector3.Lerp(A, B, ratio);
                    Vector3 toAC = Vector3.Lerp(A, C, ratio);

                    float distABtoAC = Vector3.Distance(fromAB, toAC);
                    int samplingCountABtoAC = (int)(distABtoAC / (grid * multiple)); //얘 계산 왜 이러냐... 그냥 multiple만 써도 되는거 아닌가?

                    for (int j = 0; j < samplingCountABtoAC; ++j)
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
                            if (angle >= 0 && angle < 90)            { shift = 0; }
                            else if (angle >=  90 && angle < 180)    { shift = 1; }
                            else if (angle >= 180 && angle < 270)    { shift = 2; }
                            else                                     { shift = 3; }
                        }
                        else
                        {
                            if (angle >= 0 && angle < 90)            { shift = 4; }
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
                    }
                }
            }
        }

        Debug.Log($"Sampling Done. (count:{data.Keys.Count})");
        canDraw = true;
    }
    private VoxelType GetVoxelState(Vector3 n1, Vector3 n2, Vector3 n3)
    {
        VoxelType type = VoxelType.None;

        float a1 = Mathf.Acos(Vector3.Dot(Vector3.up, n1)) * Mathf.Rad2Deg;
        float a2 = Mathf.Acos(Vector3.Dot(Vector3.up, n1)) * Mathf.Rad2Deg;
        float a3 = Mathf.Acos(Vector3.Dot(Vector3.up, n1)) * Mathf.Rad2Deg;
        Debug.Log($"normal angle? {a1}, {a2}, {a3}");

        //이걸로 계산하는게 차라리 좋을 듯?

        if ((0 < n1.y && n1.y < 0.8f)
            || (0 < n2.y && n2.y < 0.8f)
            || (0 < n3.y && n3.y < 0.8f))
        {
            type = VoxelType.Slope;
        }
        if (n1.y < 0 || n2.y < 0 || n3.y < 0)
        {
            type = VoxelType.Obstacle;
        }
        if (n1.y == 1 && n2.y == 1 & n3.y == 1)
        {
            type = VoxelType.Plain;
        }

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

                Gizmos.color = Color.black;
                //Gizmos.DrawCube(center, Vector3.one * 0.025f);
                //Gizmos.DrawLine(center, center + Vector3.up   * halfGrid);
                //Gizmos.DrawLine(center, center + Vector3.down * halfGrid);

                Vector3 dir = Vector3.zero;
                float offset = Mathf.Sqrt(2) * 0.5f * halfGrid;

                bool lowerIsDrawed = false, upperIsDrawed = false;
                for (int i = 0; i < 8; ++i)
                {
                    switch (i)
                    {
                        case 0: dir = Vector3.right * quaterGrid + Vector3.down * quaterGrid; break;
                        case 1: dir = Vector3.forward * quaterGrid + Vector3.down * quaterGrid; break;
                        case 2: dir = Vector3.left * quaterGrid + Vector3.down * quaterGrid; break;
                        case 3: dir = Vector3.back * quaterGrid + Vector3.down * quaterGrid; break;
                        case 4: dir = Vector3.right * quaterGrid + Vector3.up * quaterGrid; break;
                        case 5: dir = Vector3.forward * quaterGrid + Vector3.up * quaterGrid; break;
                        case 6: dir = Vector3.left * quaterGrid + Vector3.up * quaterGrid; break;
                        case 7: dir = Vector3.back * quaterGrid + Vector3.up * quaterGrid; break;
                    }

                    int sub_type = (data[radix].SubVoxel & (0b11 << i * 2)) >> i * 2;
                    switch ((VoxelType)sub_type)
                    {
                        case VoxelType.Plain:  Gizmos.color = new Color(0, 1, 0, alpha[0]); break;
                        case VoxelType.Slope: Gizmos.color = new Color(0, 0, 1, alpha[1]); break;
                        case VoxelType.Obstacle: Gizmos.color = new Color(1, 0, 0, alpha[2]); break;
                        default: continue;
                    }

                    if (!lowerIsDrawed && i < 4)
                    {
                        Vector3 lower = center + Vector3.down * halfGrid;
                        Gizmos.DrawLine(lower + Vector3.right * halfGrid, lower + Vector3.forward * halfGrid);
                        Gizmos.DrawLine(lower + Vector3.forward * halfGrid, lower + Vector3.left * halfGrid);
                        Gizmos.DrawLine(lower + Vector3.left * halfGrid, lower + Vector3.back * halfGrid);
                        Gizmos.DrawLine(lower + Vector3.back * halfGrid, lower + Vector3.right * halfGrid);
                        lowerIsDrawed = true;
                    }
                    else if(!upperIsDrawed)
                    {
                        Vector3 upper = center + Vector3.up * halfGrid;
                        Gizmos.DrawLine(upper + Vector3.right * halfGrid, upper + Vector3.forward * halfGrid);
                        Gizmos.DrawLine(upper + Vector3.forward * halfGrid, upper + Vector3.left * halfGrid);
                        Gizmos.DrawLine(upper + Vector3.left * halfGrid, upper + Vector3.back * halfGrid);
                        Gizmos.DrawLine(upper + Vector3.back * halfGrid, upper + Vector3.right * halfGrid);
                        upperIsDrawed = true;
                    }

                    Matrix4x4 rotationMatrix = Matrix4x4.TRS(center + dir, Quaternion.Euler(0, 45, 0), new Vector3(offset, halfGrid, offset));
                    Gizmos.matrix = rotationMatrix;
                    Gizmos.DrawCube(Vector3.zero, Vector3.one);
                    Gizmos.matrix = Matrix4x4.identity;
                }
                //*/
            }
        }
    }
}