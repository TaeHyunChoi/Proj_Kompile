using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http.Headers;
using Unity.Burst.Intrinsics;
using UnityEngine;
using static Public;

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
        float weight = grid * (1 / samplingInterval);
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

            if (targetTransform.CompareTag("Movable"))       { type = VoxelType.Movable; }
            else if (targetTransform.CompareTag("Obstacle")) { type = VoxelType.Obstacle; }
            else { type = VoxelType.None; }

            for (int t = 0; t < triangles.Length;)
            {
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
                    int samplingCountABtoAC = (int)(distABtoAC / (grid * multiple));

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

                        //와.. 참조가 많아지니까 '체감될 정도로' 속도가 느려지는구나 ㄷㄷ;
                        if(type == VoxelType.Obstacle)
                        {
                            Vector3 l1 = normals[triangles[t - 3]];
                            Vector3 l2 = normals[triangles[t - 2]];
                            Vector3 l3 = normals[triangles[t - 1]];

                            //맵은 y축 회전 정도만 있을텐데 world 좌표 변환이 필요 없다.
                            // Vector3 w1 = transform.TransformDirection(l1);
                            // Vector3 w2 = transform.TransformDirection(l2);
                            // Vector3 w3 = transform.TransformDirection(l3);

                            if (l1.y > 0 && l2.y > 0 && l3.y > 0)
                            // if (w1.y > 0 && w2.y > 0 && w3.y > 0)
                            {
                                typeBits = (int)VoxelType.Movable << shift;
                                sub = data[radix].SubVoxel & ~mask;
                                sub |= typeBits;
                                data[radix] = new Voxel_t(sub);
                                continue;
                            }
                        }
                        
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
                Gizmos.DrawCube(center, Vector3.one * 0.025f);

                //*
                Vector3 dir = Vector3.zero;
                for (int i = 0; i < 8; ++i)
                {
                    switch (i)
                    {
                        case 0: dir = new Vector3(quaterGrid, -quaterGrid, quaterGrid); break;
                        case 1: dir = new Vector3(-quaterGrid, -quaterGrid, quaterGrid); break;
                        case 2: dir = new Vector3(-quaterGrid, -quaterGrid, -quaterGrid); break;
                        case 3: dir = new Vector3(quaterGrid, -quaterGrid, -quaterGrid); break;
                        case 4: dir = new Vector3(quaterGrid, quaterGrid, quaterGrid); break;
                        case 5: dir = new Vector3(-quaterGrid, quaterGrid, quaterGrid); break;
                        case 6: dir = new Vector3(-quaterGrid, quaterGrid, -quaterGrid); break;
                        case 7: dir = new Vector3(quaterGrid, quaterGrid, -quaterGrid); break;
                    }

                    int sub_type = (data[radix].SubVoxel & (0b11 << i * 2)) >> i * 2;
                    switch ((VoxelType)sub_type)
                    {
                        case VoxelType.Movable:  Gizmos.color = Color.green;    break;
                        case VoxelType.Obstacle: Gizmos.color = Color.red;      break;
                        default: continue;
                    }
                    Gizmos.DrawCube(center + dir, Vector3.one * 0.025f);
                    Gizmos.DrawLine(center, center + dir);
                }
                //*/
            }
        }
    }
}