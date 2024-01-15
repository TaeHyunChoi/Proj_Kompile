using System.Collections.Generic;
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

    //주로 사용하는 게 Vector3 이므로 Job system  사용도 가능할 것 같은데?
    private void Sampling()
    {
        float weight = voxelSize * (1/samplingIntervalWeight);
        VoxelType type = VoxelType.None;

        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            Mesh mesh = filter[f].mesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector3 A, B, C;
            Vector3 epsilon = Vector3.one * 0.00001f;

            type = VoxelType.None;
            if (targetTransform.CompareTag("Movable"))  
            { 
                type = VoxelType.Movable; 
            }
            else if (targetTransform.CompareTag("Obstacle")) 
            { 
                type = VoxelType.Obstacle; 
            }
            
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

                        float voxelSize_invert = 1f / voxelSize;
                        float cx = Mathf.Floor(samplingPoint.x * voxelSize_invert) * voxelSize;
                        float cy = Mathf.Floor(samplingPoint.y * voxelSize_invert) * voxelSize;
                        float cz = Mathf.Floor(samplingPoint.z * voxelSize_invert) * voxelSize;
                        Vector3 clamped = new Vector3(cx, cy, cz);

                        int bx = (int)(cx * voxelSize_invert);
                        int by = (int)(cy * voxelSize_invert);
                        int bz = (int)(cz * voxelSize_invert);
                        int radix = (bx << 16) | (by << 8) | bz;

                        //register voxel
                        if (!data.ContainsKey(radix))
                        {
                            Voxel_t voxel = new Voxel_t(type, 0x0000);
                            data.Add(radix, voxel);
                        }

                        //type: change voxel type to upper level
                        else if(type > data[radix].Type)
                        {
                            data[radix] = new Voxel_t(type, data[radix].Sub);
                        }

                        //sub:
                        float halfVoxelSize = voxelSize * 0.5f;
                        Vector3 diff = samplingPoint - clamped;
                        int d = 0;
                        if (diff.x > halfVoxelSize) { d |= 1 << 2; }
                        if (diff.y > halfVoxelSize) { d |= 1 << 1; }
                        if (diff.z > halfVoxelSize) { d |= 1 << 0; }

                        int shift;
                        switch (d)
                        {
                            // case 0b_000: shift =  0; break; //[-, -, -]
                            default:     shift =  0; break;
                            case 0b_100: shift =  2; break; //[+, -, -]
                            case 0b_001: shift =  4; break; //[-, -, +]
                            case 0b_101: shift =  6; break; //[+, -, +]
                            case 0b_010: shift =  8; break; //[-, +, -] 
                            case 0b_110: shift = 10; break; //[+, +, -]
                            case 0b_011: shift = 12; break; //[-, +, +]
                            case 0b_111: shift = 14; break; //[+, +, +]
                        }

                        int sub = data[radix].Sub & ~(0b11 << shift);
                        sub |= (int)type << shift;

                        if(sub > data[radix].Sub)
                        {
                            data[radix] = new Voxel_t(type, sub);
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

        if (drawGrids)
        {
            Vector3 subPos = Vector3.one * voxelSize * 0.25f;
            for (int x = 0; x < 512; ++x)
            {
                for (int y = 0; y < 64; ++y)
                {
                    for (int z = 0; z < 512; ++z)
                    {
                        Gizmos.color = new Color(1, 1, 0, 0.10f);
                        Gizmos.DrawWireCube(subPos + new Vector3(x, y, z) * voxelSize * 0.5f, Vector3.one * voxelSize * 0.5f);
                    }
                }
            }

            // draw voxel grids
            Vector3 pos = Vector3.one * voxelSize * 0.5f;
            for (int x = 0; x < 16; ++x)
            {
                for (int y = 0; y < 16; ++y)
                {
                    for (int z = 0; z < 16; ++z)
                    {
                        Gizmos.color = new Color(1, 1, 1, 0.50f);
                        Gizmos.DrawWireCube(pos + new Vector3(x, y, z) * voxelSize, Vector3.one * voxelSize);
                    }
                }
            }
        }

        //draw voxel
        foreach (int radix in data.Keys)
        {
            float x = radix >> 16;
            float y = (radix & 0xFF00) >> 8;
            float z = radix & 0xFF;
            
            Vector3 center = new Vector3(x, y, z) * voxelSize + Vector3.one * voxelSize * 0.5f;
            for (int i = 0; i < 8; ++i)
            {
                Vector3 subCenter = center;
                float unit = voxelSize * 0.25f;
                switch (i)
                {
                    case 0: subCenter = center + new Vector3(-unit, -unit, -unit); break;
                    case 1: subCenter = center + new Vector3( unit, -unit, -unit); break;
                    case 2: subCenter = center + new Vector3(-unit, -unit,  unit); break;
                    case 3: subCenter = center + new Vector3( unit, -unit,  unit); break;
                    case 4: subCenter = center + new Vector3(-unit,  unit, -unit); break;
                    case 5: subCenter = center + new Vector3( unit,  unit, -unit); break;
                    case 6: subCenter = center + new Vector3(-unit,  unit,  unit); break;
                    case 7: subCenter = center + new Vector3( unit,  unit,  unit); break;
                }

                int sub_type = (data[radix].Sub & (0b11 << i * 2)) >> i * 2;
                switch ((VoxelType)sub_type)
                {
                    case VoxelType.None:     Gizmos.color = new Color(0, 0, 0, 0);      break;
                    case VoxelType.Movable:  Gizmos.color = new Color(0, 1, 0, 0.10f);  break;
                    case VoxelType.Obstacle: Gizmos.color = new Color(1, 0, 0, 0.25f);  break;
                }
                Gizmos.DrawCube(subCenter, Vector3.one * voxelSize * 0.5f);
                Gizmos.DrawWireCube(subCenter, Vector3.one * voxelSize * 0.5f);
            }
        }
    }
}