using System;
using System.Collections.Generic;
using UnityEngine;

public class MapSampler : MonoBehaviour
{
    [Header("Voxels")]
    [SerializeField] private GameObject resource;
    [SerializeField] private float voxelSize;
    [SerializeField] private float samplingIntervalWeight;
    [Header("Grids")]
    [SerializeField] private bool drawGrids;
    private Dictionary<int, byte> data;
    MeshFilter[] filter;


    private List<Vector3> samplingData = new List<Vector3>();
    private List<Vector3> clampedData = new List<Vector3>();
    private bool canDraw;

    private void Awake()
    {
        data = new Dictionary<int, byte>();
        filter = resource.GetComponentsInChildren<MeshFilter>();
    }
    private void Start()
    {
        Sampling();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            samplingData.Clear();
            filter = resource.GetComponentsInChildren<MeshFilter>();
            Sampling();
        }
    }
    private void Sampling()
    {
        float weight = voxelSize * (1/samplingIntervalWeight);
        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            Mesh mesh = filter[f].mesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector3 A, B, C;

            for (int t = 0; t < triangles.Length;)
            {
                A = targetTransform.TransformPoint(vertices[triangles[t++]]);
                B = targetTransform.TransformPoint(vertices[triangles[t++]]);
                C = targetTransform.TransformPoint(vertices[triangles[t++]]);

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
                        Vector3 samplingPoint = Vector3.Lerp(fromAB, toAC, (float)j / samplingCountABtoAC);
                        samplingData.Add(samplingPoint);

                        //여기서 radix로 잘 변경해야 하는데...
                        //서브 복셀 단위로 clamp 하되 + voxel로 (x,y,z)가 나머지 없이 나뉘면 Key voxel 인건가?
                        //나머지가 있다 => 서브 복셀 크기만큼을 빼면 Key voxel position이 나온다?
                        //한 번 더 검토 필요
                    }
                }
            }
        }
        Debug.Log("Sampling Done.");
        canDraw = true;
    }
    private int GetRadix(Vector3 v)
    {
        float multiple = 1 / voxelSize;
        byte bx = (byte)(v.x * multiple);
        byte by = (byte)(v.y * multiple);
        byte bz = (byte)(v.z * multiple);

        int radix = (bx << 16) | (by << 8) | (bz << 0);
        return radix;
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
            for (int x = 0; x < 32; ++x)
            {
                for (int y = 0; y < 32; ++y)
                {
                    for (int z = 0; z < 32; ++z)
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


        //draw sampling coordinate
        for (int i = 0; i < samplingData.Count; ++i)
        {
            Vector3 center = samplingData[i];
            Gizmos.color = new Color(0, 0, 1f, 1f);
            Gizmos.DrawCube(center, Vector3.one * 0.010f);
            Gizmos.DrawWireCube(center, Vector3.one * 0.010f);
        }

        //draw clamped coordinate
        for (int i = 0; i < clampedData.Count; ++i)
        {
            Vector3 center = clampedData[i];
            Gizmos.color = new Color(1f, 0, 0, 1f);
            Gizmos.DrawCube(center, Vector3.one * 0.010f);
            Gizmos.DrawWireCube(center, Vector3.one * 0.010f);
        }

        return;
        //draw voxel
        foreach (int radix in data.Keys)
        {
            float x = radix >> 16;
            float y = (radix & 0xFF00) >> 8;
            float z = radix & 0xFF;

            byte sub = data[radix];
            Vector3 center = new Vector3(x, y, z) * voxelSize + Vector3.one * voxelSize * 0.5f;

            Gizmos.color = new Color(0, 0, 1, 0.1f);
            Gizmos.DrawCube(center, Vector3.one * voxelSize);
            Gizmos.DrawWireCube(center, Vector3.one * voxelSize);
            //continue;

            Vector3 subCenter = center;
            for (int i = 0; i < 8; ++i)
            {
                if ((sub & (1 << i)) != 0)
                {
                    Gizmos.color = new Color(1, 0, 0, 0.25f); //red
                }
                else
                {
                    Gizmos.color = new Color(0, 1, 0, 0.25f); //???
                }

                float unit = voxelSize * 0.25f;
                switch (i)
                {
                    case 0: subCenter = center + new Vector3(-unit, -unit, -unit); break;
                    case 1: subCenter = center + new Vector3(unit, -unit, -unit); break;
                    case 2: subCenter = center + new Vector3(-unit, -unit, unit); break;
                    case 3: subCenter = center + new Vector3(unit, -unit, unit); break;

                    case 4: subCenter = center + new Vector3(-unit, unit, -unit); break;
                    case 5: subCenter = center + new Vector3(unit, unit, -unit); break;
                    case 6: subCenter = center + new Vector3(-unit, unit, unit); break;
                    case 7: subCenter = center + new Vector3(unit, unit, unit); break;
                }

                Gizmos.DrawCube(subCenter, Vector3.one * voxelSize * 0.5f);
                Gizmos.DrawWireCube(subCenter, Vector3.one * voxelSize * 0.5f);
            }
        }
    }
}