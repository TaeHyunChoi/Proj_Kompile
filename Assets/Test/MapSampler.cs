using System;
using System.Collections.Generic;
using UnityEngine;

public class MapSampler : MonoBehaviour
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

            Vector3 v1, v2, v3;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                v1 = targetTransform.TransformPoint(vertices[triangles[t]]);
                v2 = targetTransform.TransformPoint(vertices[triangles[t + 1]]);
                v3 = targetTransform.TransformPoint(vertices[triangles[t + 2]]);

                Vector3 vBase = v2 - v1, vSide = v3 - v1;

                Vector3 baseDir = vBase.normalized;
                Vector3 sideDir = vSide.normalized;

                float dist = vBase.magnitude;
                float delta = 0;

                while (true)
                {
                    Vector3 pos = v1 + (baseDir * delta);

                    //1 side
                    Vector3 side = vSide * Mathf.Abs(((dist - delta) / dist));
                    float sideDist = side.magnitude;
                    float sideDelta = 0;

                    while (true)
                    {
                        Vector3 samplingPoint = pos + (sideDir * sideDelta);
                        int radix = GetRadix(samplingPoint);

                        if (!data.ContainsKey(radix))
                        {
                            data.Add(radix, type);
                        }
                        else if (data[radix] < type)
                        {
                            data[radix] = type;
                        }

                        sideDelta += samplingInterval;
                        if (sideDelta > sideDist)
                            break;
                    }

                    delta += samplingInterval;
                    if (delta > dist)
                        break;
                }
            }
        }

        Debug.Log("Sampling Done.");
        canDraw = true;
    }
    private int GetRadix(Vector3 v)
    {
        byte bx = (byte)(v.x * intervalUnit);
        byte by = (byte)(v.y * intervalUnit);
        byte bz = (byte)(v.z * intervalUnit);

        int radix = (bx << 16) | (by << 8) | (bz << 0);
        return radix;
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

                Vector3 center = new Vector3(x, y, z) * samplingInterval + Vector3.one * samplingInterval * 0.5f;
                //Debug.Log($"clamp:{new Vector3(x, y, z) * samplingInterval} => center:{center}");

                switch (data[radix])
                {
                    case 2:
                        Gizmos.color = new Color(1, 0, 0, 0.3f);
                        Gizmos.DrawCube(center, Vector3.one * samplingInterval);
                        Gizmos.DrawWireCube(center, Vector3.one * samplingInterval);
                        break;
                    case 1:
                        Gizmos.color = new Color(0, 1, 0, 0.3f);
                        Gizmos.DrawCube(center, Vector3.one * samplingInterval);
                        Gizmos.DrawWireCube(center, Vector3.one * samplingInterval);
                        break;
                    case 0:
                        Gizmos.color = new Color(0, 0, 0, 0.3f);
                        // Gizmos.DrawCube(pos, Vector3.one * samplingInterval);
                        Gizmos.DrawWireCube(center, Vector3.one * samplingInterval);
                        break;
                }
            }
        }
    }
}