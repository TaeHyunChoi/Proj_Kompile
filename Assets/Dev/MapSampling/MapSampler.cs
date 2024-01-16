using System;
using System.Collections.Generic;
using System.Data;
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
        float halfVoxel_invert = 1 / halfVoxelSize;

        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            Mesh mesh = filter[f].mesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            
            if (targetTransform.CompareTag("Movable"))  { type = VoxelType.Movable; }
            else if (targetTransform.CompareTag("Obstacle")) { type = VoxelType.Obstacle; }
            else  { type = VoxelType.None; }
            
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

                        //굉장히 의심스럽다 친구야..?
                        int cx = Mathf.CeilToInt(samplingPoint.x * halfVoxel_invert);
                        int cy = Mathf.CeilToInt(samplingPoint.y * halfVoxel_invert);
                        int cz = Mathf.CeilToInt(samplingPoint.z * halfVoxel_invert);
                        
                        //x,z는 짝수가 나오면 안되네?
                        //x,z 둘 다 홀수이거나 x,z 둘 다 짝수이거나
                        //둘 중 하나라도 성립 안하면 간격에 맞지 않아.
                        bool ex = cx / 2 == 0;
                        bool ez = cz / 2 == 0;
                        if(!((ex & ez) || (!ex & !ez)))
                            continue;

                        //register voxel
                        int radix = (cx << 16) | (cy << 8) | cz;
                        if (!data.ContainsKey(radix))
                        {
                            Voxel_t voxel = new Voxel_t(type, 0x0000);
                            data.Add(radix, voxel);
                        }

                        //sub-voxel을 구하기 위한 방정식
                        Vector3 voxelPoint = new Vector3(cx, cy, cz) * halfVoxelSize;
                        float p = voxelPoint.x - voxelPoint.z;  //z =  x - p
                        float q = voxelPoint.x + voxelPoint.z;  //z = -x + q;

                        //sub-voxel의 상대 좌표
                        bool bx = samplingPoint.z >= samplingPoint.x - p;
                        bool by = samplingPoint.y >= voxelPoint.y;
                        bool bz = samplingPoint.z >= -samplingPoint.x + q;

                        int shift;
                        if(by == false)
                        {
                            if     (!bx & !bz) { shift = 0; }
                            else if( bx & !bz) { shift = 1; }
                            else if( bx &  bz) { shift = 2; }
                            else               { shift = 3; }
                        }
                        else
                        {
                            if     (!bx & !bz) { shift = 4; }
                            else if( bx & !bz) { shift = 5; }
                            else if( bx &  bz) { shift = 6; }
                            else               { shift = 7; }
                        }
                        shift *= 2;
                        
                        //change [voxel type] to upper level.
                        if(type > data[radix].Type)
                        {
                            data[radix] = new Voxel_t(type, data[radix].Sub);
                        }

                        //change [sub-voxel type] to upper level.
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

        if (drawGrids)
        {
            //draw voxel
            foreach (int radix in data.Keys)
            {
                float x = radix >> 16;
                float y = (radix & 0xFF00) >> 8;
                float z = radix & 0xFF;

                Vector3 center = new Vector3(x, y, z) * (voxelSize * 0.5f);
                for (int i = 0; i < 8; ++i)
                {
                    Vector3 subCenter = center;
                    float unit = voxelSize * 0.25f;

                    switch (i)
                    {
                        case 0: subCenter = center + new Vector3(0f, -unit, -unit); break;
                        case 1: subCenter = center + new Vector3(unit, -unit, 0f); break;
                        case 2: subCenter = center + new Vector3(unit, -unit, unit); break;
                        case 3: subCenter = center + new Vector3(-unit, -unit, 0f); break;
                        case 4: subCenter = center + new Vector3(0f, unit, -unit); break;
                        case 5: subCenter = center + new Vector3(unit, unit, 0f); break;
                        case 6: subCenter = center + new Vector3(unit, unit, unit); break;
                        case 7: subCenter = center + new Vector3(-unit, unit, 0f); break;
                    }

                    int sub_type = (data[radix].Sub & (0b11 << i * 2)) >> i * 2;
                    switch ((VoxelType)sub_type)
                    {
                        case VoxelType.None: Gizmos.color = new Color(0, 0, 0, 0); break;
                        case VoxelType.Movable: Gizmos.color = new Color(0, 1, 0, 0.10f); break;
                        case VoxelType.Obstacle: Gizmos.color = new Color(1, 0, 0, 0.25f); break;
                    }

                    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                    Vector3 localPosition = transform.InverseTransformPoint(subCenter); // 전역 좌표를 로컬 좌표로 변환
                    Gizmos.matrix *= Matrix4x4.TRS(localPosition, Quaternion.Euler(0f, 45f, 0f), Vector3.one * (voxelSize * 0.5f) * Mathf.Sqrt(2));


                    Gizmos.DrawCube(Vector3.zero, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

                    Gizmos.matrix = Matrix4x4.identity; // Reset matrix to avoid affecting other Gizmos calls
                }
            }
        }
    }
}