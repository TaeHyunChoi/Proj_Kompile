using System;
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

    private Dictionary<int, int> data;
    private MeshFilter[] filter;

    private void Awake()
    {
        data = new Dictionary<int, int>();
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

            if      (targetTransform.CompareTag("Movable"))  
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

                        float cx = Mathf.Floor(samplingPoint.x / voxelSize) * voxelSize;
                        float cy = Mathf.Floor(samplingPoint.y / voxelSize) * voxelSize;
                        float cz = Mathf.Floor(samplingPoint.z / voxelSize) * voxelSize;
                        Vector3 clamped = new Vector3(cx, cy, cz);

                        byte bx = (byte)(cx / voxelSize);
                        byte by = (byte)(cy / voxelSize);
                        byte bz = (byte)(cz / voxelSize);
                        int radix = (bx << 16) | (by << 8) | bz;

                        int type_i = (int)type << 8;
                        if (!data.ContainsKey(radix))
                        {
                            data.Add(radix, type_i);
                        }
                        else if((int)type > (data[radix] >> 8))
                        {
                            data[radix] &= 0xFF00;
                            data[radix] |= type_i;
                        }

                        //voxel sub point
                        float halfVoxelSize = voxelSize * 0.5f;
                        Vector3 diff = samplingPoint - clamped;
                        byte d = 0;
                        if (diff.x > halfVoxelSize) { d |= 1 << 2; }
                        if (diff.y > halfVoxelSize) { d |= 1 << 1; }
                        if (diff.z > halfVoxelSize) { d |= 1 << 0; }

                        //on/off 개념이 아니라 높은 숫자로 바꾼다! 개념으로 가는 게 맞으려나?
                        //그렇다면.. none, move, obstacle, ... 등으로 사용 메모리를 늘려야 한다.
                        //ㅇㅋ... 시도해보고...
                        //해당 자리값이 이미 obstacle이면? 아랫 단계인 none, move는 불가하다! 라는 말이잖어?
                        //말은 되는데 흠...
                        //다음 복셀도 Obstacle이라면 forcedDir을 유지하는건 어떨랑가.

                        if((data[radix] >> 8) == (int)VoxelType.Movable)
                        {
                            switch(d)
                            {
                                case 0b_000: data[radix] |= 1 << 0; break; //[-, -, -]
                                case 0b_100: data[radix] |= 1 << 1; break; //[+, -, -]
                                case 0b_001: data[radix] |= 1 << 2; break; //[-, -, +]
                                case 0b_101: data[radix] |= 1 << 3; break; //[+, -, +]
                                case 0b_010: data[radix] |= 1 << 4; break; //[-, +, -]
                                case 0b_110: data[radix] |= 1 << 5; break; //[+, +, -]
                                case 0b_011: data[radix] |= 1 << 6; break; //[-, +, +]
                                case 0b_111: data[radix] |= 1 << 7; break; //[+, +, +]
                            }
                        }
                        else //Movable 외의 타입은 이동 불가(0)으로 비트 마스킹
                        {
                            int compare = 0x00;
                            switch(d)
                            {
                                case 0b_000: compare = 1 << 0; break; //[-, -, -]
                                case 0b_100: compare = 1 << 1; break; //[+, -, -]
                                case 0b_001: compare = 1 << 2; break; //[-, -, +]
                                case 0b_101: compare = 1 << 3; break; //[+, -, +]
                                case 0b_010: compare = 1 << 4; break; //[-, +, -]
                                case 0b_110: compare = 1 << 5; break; //[+, +, -]
                                case 0b_011: compare = 1 << 6; break; //[-, +, +]
                                case 0b_111: compare = 1 << 7; break; //[+, +, +]
                            }
                            compare ^= 0x00;
                            data[radix] &= compare;
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

            int info = data[radix];
            byte sub = (byte)(info & 0xFF);

            Vector3 center = new Vector3(x, y, z) * voxelSize + Vector3.one * voxelSize * 0.5f;

            for (int i = 0; i < 8; ++i)
            {
                if ((sub & (1 << i)) != 0)
                {
                    Gizmos.color = new Color(0, 1, 0, 0.10f); //???

                }
                else
                {
                    Gizmos.color = new Color(1, 0, 0, 0f); //null
                }

                Vector3 subCenter = center;
                float unit = voxelSize * 0.25f;
                switch (i)
                {
                    case 0: subCenter = center + new Vector3(-unit, -unit, -unit); break;
                    case 1: subCenter = center + new Vector3( unit, -unit, -unit); break;
                    case 2: subCenter = center + new Vector3(-unit, -unit,  unit); break;
                    case 3: subCenter = center + new Vector3( unit, -unit,  unit); break;

                    case 4: subCenter = center + new Vector3(-unit, unit, -unit); break;
                    case 5: subCenter = center + new Vector3( unit, unit, -unit); break;
                    case 6: subCenter = center + new Vector3(-unit, unit,  unit); break;
                    case 7: subCenter = center + new Vector3( unit, unit,  unit); break;
                }

                Gizmos.DrawCube(subCenter, Vector3.one * voxelSize * 0.5f);
                Gizmos.DrawWireCube(subCenter, Vector3.one * voxelSize * 0.5f);
            }
        }
    }
}