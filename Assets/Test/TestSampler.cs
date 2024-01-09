using System;
using System.Collections.Generic;
using UnityEngine;

public class TestSampler : MonoBehaviour
{
    [SerializeField] private float intervalUnit;
    private float samplingInterval;
    [SerializeField] private GameObject resource;
    private Dictionary<int, byte> data;
    MeshFilter[] filter;

    private bool canDraw;

    private void Awake()
    {
        data = new Dictionary<int, byte>();
        samplingInterval = 1 / intervalUnit;
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

            byte type;
            if (targetTransform.CompareTag("Obstacle"))
                type = 2;
            else if (targetTransform.CompareTag("Movable"))
                type = 1;
            else
                type = 0;

            Mesh mesh = filter[i].mesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            Vector3 pos = targetTransform.position;
            Vector3 v1, v2, v3;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                v1 = targetTransform.TransformPoint(vertices[triangles[t]]);
                v2 = targetTransform.TransformPoint(vertices[triangles[t + 1]]);
                v3 = targetTransform.TransformPoint(vertices[triangles[t + 2]]);

                // Debug.Log($"#{t / 3}\nv1{v1}, v2{v2}, v3{v3}");
                // Debug.Log($"triangle: {triangles[t]}, {triangles[t + 1]}, {triangles[t + 2]}");

                int radix = GetRadix(v1);
                if (!data.ContainsKey(radix))
                {
                    data.Add(radix, type);
                }
                radix = GetRadix(v2);
                if (!data.ContainsKey(radix))
                {
                    data.Add(radix, type);
                }
                radix = GetRadix(v3);
                if (!data.ContainsKey(radix))
                {
                    data.Add(radix, type);
                }

                continue;
                SamplePointsInTriangle(v1, v2, v3, type);
            }
        }

        Debug.Log("Sampling Done.");

        foreach (var radix in data.Keys)
        {
            float x = radix >> 16;
            float y = (radix & 0xFF00) >> 8;
            float z = radix & 0xFF;
            Vector3 pos = new Vector3(x, y, z) * samplingInterval;

            // Debug.Log($"[{radix}] {pos}");
        }
        canDraw = true;
    }

    private int GetRadix(Vector3 v)
    {
        byte bx = (byte)(v.x * intervalUnit);
        byte by = (byte)(v.y * intervalUnit);
        byte bz = (byte)(v.z * intervalUnit);

        Debug.Log($"{v} => [{bx},{by},{bz}]\n=> ({bx * samplingInterval}, {by * samplingInterval}, {bz * samplingInterval})");
        int radix = (bx << 16) | (by << 8) | (bz << 0);
        return radix;
    }

    private void SamplePointsInTriangle(Vector3 v1, Vector3 v2, Vector3 v3, byte type)
    {
        float minX = Min(v1.x, v2.x, v3.x);
        float minY = Min(v1.y, v2.y, v3.y);
        float minZ = Min(v1.z, v2.z, v3.z);

        float maxX = Max(v1.x, v2.x, v3.x);
        float maxY = Max(v1.y, v2.y, v3.y);
        float maxZ = Max(v1.z, v2.z, v3.z);

        // Debug.Log($"min({minX}, {minY}, {minZ}), max({maxX}, {maxY}, {maxZ})");
        // return;

        for (float x = minX; x <= maxX; x += samplingInterval)
        {
            for (float y = minY; y <= maxY; y += samplingInterval)
            {
                for (float z = minZ; z <= maxZ; z += samplingInterval)
                {
                    Vector3 samplePoint = new Vector3(x, y, z);
                    Debug.Log(samplePoint);
                    if (!IsPointInTriangle(samplePoint, v1, v2, v3))
                    {
                        type = 0;
                    }

                    byte bx = (byte)(x * intervalUnit);
                    byte by = (byte)(y * intervalUnit);
                    byte bz = (byte)(z * intervalUnit);

                    int radix = (bx << 16) | (by << 8) | (bz << 0);

                    if (!data.ContainsKey(radix))
                    {
                        data.Add(radix, type);
                    }
                    else if (data[radix] < type)
                    {
                        data[radix] = type;
                    }
                }
            }
        }
    }
    private bool IsPointInTriangle(Vector3 p, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float epsilon = 0.0001f;
        float denominator = (p2.y - p3.y) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.y - p3.y);

        if (Mathf.Abs(denominator) < epsilon)
        {
            return true;
        }

        float a = ((p2.y - p3.y) * (p.x - p3.x) + (p3.x - p2.x) * (p.y - p3.y)) / denominator;
        float b = ((p3.y - p1.y) * (p.x - p3.x) + (p1.x - p3.x) * (p.y - p3.y)) / denominator;
        float c = 1 - a - b;

        return a >= epsilon && a <= 1 + epsilon && b >= epsilon && b <= 1 + epsilon && c >= epsilon && c <= 1 + epsilon;
    }

    private float Min(params float[] f)
    {
        float min = f[0];
        for (int i = 1; i < f.Length; ++i)
        {
            if (f[i] < min)
            {
                min = f[i];
            }
        }

        //부동소수점 문제에서 벗어나기 위한 노력..
        int minInt = (int)(min * 100);
        int sampleInt = (int)(samplingInterval * 100);
        minInt /= sampleInt;
        
        return minInt * samplingInterval;
    }
    private float Max(params float[] f)
    {
        float max = f[0];
        for (int i = 1; i < f.Length; ++i)
        {
            if (f[i] > max)
            {
                max = f[i];
            }
        }

        int maxInt = (int)(max * 100);
        int sampleInt = (int)(samplingInterval * 100);
        maxInt /= sampleInt;

        return maxInt * samplingInterval;
    }


    private void OnDrawGizmos()
    {
        if (canDraw)
        {
            foreach (int radix in data.Keys)
            {
                float x = radix >> 16;
                float y = (radix & 0xFF00) >> 8;
                float z = radix & 0xFF;
                Vector3 pos = new Vector3(x, y, z) * samplingInterval;

                switch (data[radix])
                {
                    case 2:
                        Gizmos.color = new Color(1, 0, 0, 0.3f);
                        Gizmos.DrawCube(pos, Vector3.one * samplingInterval);
                        Gizmos.DrawWireCube(pos, Vector3.one * samplingInterval);
                        break;
                    case 1:
                        Gizmos.color = new Color(0, 1, 0, 0.3f);
                        Gizmos.DrawCube(pos, Vector3.one * samplingInterval);
                        Gizmos.DrawWireCube(pos, Vector3.one * samplingInterval);
                        break;
                    case 0:
                        Gizmos.color = new Color(0, 0, 0, 0.3f);
                        // Gizmos.DrawCube(pos, Vector3.one * samplingInterval);
                        Gizmos.DrawWireCube(pos, Vector3.one * samplingInterval);
                        break;
                }
            }
        }
    }
}