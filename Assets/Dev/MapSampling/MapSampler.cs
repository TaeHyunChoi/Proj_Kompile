using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;

public class MapSampler : MonoBehaviour
{
    [Header("File")]
    [SerializeField] private string fileName;

    [Header("Voxels")]
    [SerializeField] private Transform resourceTransform;
    [SerializeField] private float voxelSize;
    [SerializeField] private float samplingIntervalWeight;

    [Header("Grids")]
    [SerializeField] private bool drawGrids;
    private bool canDraw;

    private Dictionary<int, Voxel_t> data;
    private MeshFilter[] filter;


    private void Awake()
    {
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

        Sampling();
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
            Sampling();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            data.Clear();
            filter = resourceTransform.GetComponentsInChildren<MeshFilter>();
            Sampling();
        }
    }


    private void Sampling()
    {
        float weight = voxelSize * (1 / samplingIntervalWeight);
        VoxelType type;
        Vector3 epsilon = Vector3.one * 0.00001f;
        Vector3 A, B, C;
        float halfVoxelSize = voxelSize * 0.5f;
        float voxel_invert = 1 / voxelSize;

        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            Mesh mesh = filter[f].mesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            if (targetTransform.CompareTag("Movable")) { type = VoxelType.Movable; }
            else if (targetTransform.CompareTag("Obstacle")) { type = VoxelType.Obstacle; }
            else { type = VoxelType.None; }

            for (int t = 0; t < triangles.Length;)
            {
                A = targetTransform.TransformPoint(vertices[triangles[t++]]) + epsilon;
                B = targetTransform.TransformPoint(vertices[triangles[t++]]) + epsilon;
                C = targetTransform.TransformPoint(vertices[triangles[t++]]) + epsilon;

                float distAB = Vector3.Distance(A, B);
                float multiple = (distAB < voxelSize) ? weight * 0.25f : weight;
                int samplingCountAB = (int)(distAB / (voxelSize * multiple));
                for (int i = 0; i < samplingCountAB; ++i)
                {
                    float ratio = (float)i / samplingCountAB;
                    Vector3 fromAB = Vector3.Lerp(A, B, ratio);
                    Vector3 toAC = Vector3.Lerp(A, C, ratio);

                    float distABtoAC = Vector3.Distance(fromAB, toAC);
                    int samplingCountABtoAC = (int)(distABtoAC / (voxelSize * multiple));

                    for (int j = 0; j < samplingCountABtoAC; ++j)
                    {
                        //voxel key point
                        Vector3 samplingPoint = Vector3.Lerp(fromAB, toAC, (float)j / samplingCountABtoAC);
                        int cx = Mathf.FloorToInt(samplingPoint.x * voxel_invert);
                        int cy = Mathf.FloorToInt(samplingPoint.y * voxel_invert);
                        int cz = Mathf.FloorToInt(samplingPoint.z * voxel_invert);
                        int radix = cx << 16 | cy << 8 | cz;

                        Vector3 clampPoint = new Vector3(cx, cy, cz) * voxelSize;
                        Vector3 center = clampPoint + Vector3.one * halfVoxelSize;
                        Vector3 side = samplingPoint - center;

                        //set key-voxel data
                        if (!data.ContainsKey(radix))
                        {
                            float radian = Mathf.Atan2(side.z, side.x);
                            float dist = Mathf.Tan(radian) * halfVoxelSize;

                            //samplingPoint가 target voxel 안에 들어있다면 추가
                            if (new Vector3(side.x, 0f, side.z).magnitude <= Mathf.Abs(dist))
                            {
                                data.Add(radix, new Voxel_t(type, 0x0000));
                            }

                            //voxel 안에 없다면 neighbor voxel을 한 번 더 탐색
                            else
                            {
                                if (radian >= 0 && radian > Mathf.PI * 0.5f)
                                { samplingPoint += new Vector3(1, 0, 1) * halfVoxelSize; }

                                else
                                if (radian >= Mathf.PI * 0.5f && radian > Mathf.PI)
                                { samplingPoint += new Vector3(-1, 0, 1) * halfVoxelSize; }

                                else
                                if (radian >= Mathf.PI && radian > Mathf.PI * 1.5f)
                                { samplingPoint += new Vector3(-1, 0, -1) * halfVoxelSize; }

                                else
                                { samplingPoint += new Vector3(1, 0, -1) * halfVoxelSize; }

                                cx = Mathf.FloorToInt(samplingPoint.x * voxel_invert);
                                cy = Mathf.FloorToInt(samplingPoint.y * voxel_invert);
                                cz = Mathf.FloorToInt(samplingPoint.z * voxel_invert);
                                radix = cx << 16 | cy << 8 | cz;

                                if (!data.ContainsKey(radix))
                                {
                                    data.Add(radix, new Voxel_t(type, 0x0000));
                                }
                            }
                        }
                        
                        // key-voxle.Type
                        if (type > data[radix].Type)
                        {
                            data[radix] = new Voxel_t(type, data[radix].Sub);
                        }

                        // sub-voxel.Type
                        Vector3 voxelPoint = new Vector3(cx, cy, cz) * halfVoxelSize;
                        float p = voxelPoint.x - voxelPoint.z;  //z =  x - p
                        float q = voxelPoint.x + voxelPoint.z;  //z = -x + q;

                        //sub-voxel의 상대 좌표
                        bool bx = samplingPoint.z >= samplingPoint.x - p;
                        bool by = samplingPoint.y >= voxelPoint.y;
                        bool bz = samplingPoint.z >= -samplingPoint.x + q;

                        int shift;
                        if (by == false)
                        {
                            if (!bx & !bz) { shift = 0; }
                            else if (bx & !bz) { shift = 1; }
                            else if (bx & bz) { shift = 2; }
                            else { shift = 3; }
                        }
                        else
                        {
                            if (!bx & !bz) { shift = 4; }
                            else if (bx & !bz) { shift = 5; }
                            else if (bx & bz) { shift = 6; }
                            else { shift = 7; }
                        }
                        shift *= 2;

                        int sub = (int)type << (shift);
                        if (sub > data[radix].Sub)
                        {
                            int mask = ~(0b11 << shift);
                            int newSub = data[radix].Sub & mask;
                            newSub |= sub;
                            data[radix] = new Voxel_t(type, newSub);
                        }
                    }
                }
            }
        }

        Debug.Log("Sampling Done.");
        canDraw = true;
    }
    private void OnDrawGizmos()
    {
        if (!canDraw)
        {
            return;
        }

        //이제 어떻게 그리면 좋으려나?

        if (drawGrids)
        {
            //draw voxel
            foreach (int radix in data.Keys)
            {
                float x = radix >> 16;
                float y = (radix & 0xFF00) >> 8;
                float z = radix & 0xFF;

                Vector3 center = new Vector3(x, y, z) * (voxelSize * 0.5f);

                Gizmos.color = Color.black;
                Gizmos.DrawCube(center, Vector3.one * 0.025f);

                for (int i = 0; i < 8; ++i)
                {
                    Vector3 subCenter = center;
                    float unit = voxelSize * 0.25f;

                    switch (i)
                    {
                        case 0: subCenter = center + new Vector3(0f, -unit, -unit); break;
                        case 1: subCenter = center + new Vector3(unit, -unit, 0f); break;
                        case 2: subCenter = center + new Vector3(0f, -unit, unit); break;
                        case 3: subCenter = center + new Vector3(-unit, -unit, 0f); break;
                        case 4: subCenter = center + new Vector3(0f, unit, -unit); break;
                        case 5: subCenter = center + new Vector3(unit, unit, 0f); break;
                        case 6: subCenter = center + new Vector3(0f, unit, unit); break;
                        case 7: subCenter = center + new Vector3(-unit, unit, 0f); break;
                    }

                    int sub_type = (data[radix].Sub & (0b11 << i * 2)) >> i * 2;
                    Vector3 to = center + (subCenter - center).normalized * voxelSize * 0.25f;
                    switch ((VoxelType)sub_type)
                    {
                        // case VoxelType.None: Gizmos.color = Color.white; break;
                        case VoxelType.Movable: Gizmos.color = Color.green; break;
                        case VoxelType.Obstacle: Gizmos.color = Color.red; break;
                        default: Gizmos.color = new Color(1f, 1f, 1f, 0f); break;
                    }
                    Gizmos.DrawCube(subCenter, Vector3.one * 0.025f);

                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(center, to);
                }
            }
        }
    }
}