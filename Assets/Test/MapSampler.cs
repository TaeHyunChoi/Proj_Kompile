using System;
using System.Collections.Generic;
using UnityEngine;

public class MapSampler : MonoBehaviour
{
    [SerializeField] private GameObject resource;
    [SerializeField] private float voxelSize;

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
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Sampling();
        }
    }

    private void Sampling()
    {
        for (int i = 0; i < filter.Length; ++i)
        {
            Transform targetTransform = filter[i].transform;

            bool isObstacle = targetTransform.CompareTag("Obstacle");

            Mesh mesh = filter[i].mesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            Vector3 A, B, C;

            for (int t = 0; t < triangles.Length;)
            {
                A = targetTransform.TransformPoint(vertices[triangles[t++]]);
                B = targetTransform.TransformPoint(vertices[triangles[t++]]);
                C = targetTransform.TransformPoint(vertices[triangles[t++]]);

                float distAC = Vector3.Distance(A, C);
                int samplingCountAC = (int)(distAC / voxelSize) * 2;

                for (int ii = 0; ii < samplingCountAC; ++ii)
                {
                    Vector3 fromAB = Vector3.Lerp(A, B, (float)ii / samplingCountAC);
                    Vector3 toAC   = Vector3.Lerp(A, C, (float)ii / samplingCountAC);

                    float distABtoAC = Vector3.Distance(fromAB, toAC);

                    int samplingCount = (int)(distABtoAC / voxelSize) * 2;
                    for (int jj = 0; jj <= samplingCount; ++jj)
                    {
                        Vector3 samplingPoint = Vector3.Lerp(fromAB, toAC, (float)jj / samplingCount);
                        samplingData.Add(samplingPoint);

                        float multiple = 1 / voxelSize;
                        float cx = (int)(samplingPoint.x * multiple) * voxelSize;
                        float cy = (int)(samplingPoint.y * multiple) * voxelSize;
                        float cz = (int)(samplingPoint.z * multiple) * voxelSize;
                        Debug.Log($"sampling: {samplingPoint} => clamped: ({cx}, {cy}, {cz})");

                        clampedData.Add(new Vector3(cx, cy, cz));
                    }
                }
            }
        }
        Debug.Log("Sampling Done.");
        canDraw = true;
    }
    private bool IsPointInTriangle(Vector3 p, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        float epsilon = voxelSize * 0.25f;
        float denominator = (v2.y - v3.y) * (v1.x - v3.x) + (v3.x - v2.x) * (v1.y - v3.y);

        return Mathf.Abs(denominator) < epsilon;

        //if (Mathf.Abs(denominator) < epsilon)
        //{
        //    return false;
        //}

        //float a = ((v2.y - v3.y) * (p.x - v3.x) + (v3.x - v2.x) * (p.y - v3.y)) / denominator;
        //float b = ((v3.y - v1.y) * (p.x - v3.x) + (v1.x - v3.x) * (p.y - v3.y)) / denominator;
        //float c = 1 - a - b;

        //return a >= epsilon && a <= 1 + epsilon && b >= epsilon && b <= 1 + epsilon && c >= epsilon && c <= 1 + epsilon;
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


        // draw voxel grids
        Vector3 pos = Vector3.one * voxelSize * 0.5f;
        for (int x = 0; x < 16; ++x)
        {
            for (int y = 0; y < 16; ++y)
            {
                for (int z = 0; z < 16; ++z)
                {
                    Gizmos.color = new Color(1, 1, 1, 0.025f);
                    Gizmos.DrawWireCube(pos + new Vector3(x, y, z) * voxelSize, Vector3.one * voxelSize);
                }
            }
        }


        //draw sampling coordinate
        for (int i = 0; i < samplingData.Count; ++i)
        {
            Vector3 center = samplingData[i];
            Gizmos.color = new Color(0, 0, 1f, 1f);
            Gizmos.DrawCube(center, Vector3.one * 0.025f);
            Gizmos.DrawWireCube(center, Vector3.one * 0.025f);
        }

        //draw clamped coordinate
        for (int i = 0; i < clampedData.Count; ++i)
        {
            Vector3 center = clampedData[i];
            Gizmos.color = new Color(1f, 0, 0, 1f);
            Gizmos.DrawCube(center, Vector3.one * 0.025f);
            Gizmos.DrawWireCube(center, Vector3.one * 0.025f);
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
                    case 1: subCenter = center + new Vector3( unit, -unit, -unit); break;
                    case 2: subCenter = center + new Vector3(-unit, -unit,  unit); break;
                    case 3: subCenter = center + new Vector3( unit, -unit,  unit); break;

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