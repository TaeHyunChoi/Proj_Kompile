using System;
using System.Collections.Generic;
using UnityEngine;

public class MapSampler : MonoBehaviour
{
    [SerializeField] private float intervalUnit;
    private float voxelSize;
    [SerializeField] private GameObject resource;
    private Dictionary<int, byte> data;
    MeshFilter[] filter;

    private List<Vector3> tempData = new List<Vector3>();
    private bool canDraw;

    private void Awake()
    {
        data = new Dictionary<int, byte>();
        voxelSize = 1 / intervalUnit;
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

            Vector3 A, B, C, AB, AC;
            Vector3 dirAB, dirAC;
            Vector3 samplingPoint;

            for (int t = 0; t < triangles.Length;)
            {
                A = targetTransform.TransformPoint(vertices[triangles[t++]]);
                B = targetTransform.TransformPoint(vertices[triangles[t++]]);
                C = targetTransform.TransformPoint(vertices[triangles[t++]]);

                AB = B - A;
                dirAB = AB.normalized;

                AC = C - A;
                dirAC = AC.normalized;

                float distAB = AB.magnitude;
                float deltaAB = 0;

                while (deltaAB < distAB)
                {
                    Vector3 start = A + (dirAB * deltaAB);

                    float distAC = (AC * (distAB - deltaAB) / distAB).magnitude;
                    float deltaAC = 0;

                    while (deltaAC < distAC)
                    {
                        samplingPoint = start + (dirAC * deltaAC);

                        float x, y, z;
                        x = (int)(samplingPoint.x * intervalUnit) / intervalUnit;
                        y = (int)(samplingPoint.y * intervalUnit) / intervalUnit;
                        z = (int)(samplingPoint.z * intervalUnit) / intervalUnit;
                        Vector3 voxelPoint = new Vector3(x, y, z);

                        byte sub = 0;
                        if (isObstacle)
                        {
                            //현재 복셀의 center 위치를 구한다
                            Vector3 center = voxelPoint + Vector3.one * voxelSize * 0.5f;

                            //(samplingPoint - center) 벡터를 구한다.
                            Vector3 dir = samplingPoint - center;
                            bool dx = dir.x >= 0;
                            bool dy = dir.y >= 0;
                            bool dz = dir.z >= 0;

                            //x,y,z 각 방향을 따져서 어느 sub-voxel에 위치한지 확인한다.
                            if (!dy)
                            {
                                if (!dx & !dz) { sub = 1 << 0; }
                                else if (dx & !dz) { sub = 1 << 1; }
                                else if (!dx & dz) { sub = 1 << 2; }
                                else if (dx & dz) { sub = 1 << 3; }
                            }
                            else
                            {
                                if (!dx & !dz) { sub = 1 << 4; }
                                else if (dx & !dz) { sub = 1 << 5; }
                                else if (!dx & dz) { sub = 1 << 6; }
                                else if (dx & dz) { sub = 1 << 7; }
                            }
                        }

                        tempData.Add(samplingPoint);

                        int radix = GetRadix(voxelPoint);
                        if (!data.ContainsKey(radix))
                        {
                            data.Add(radix, 0);
                        }
                        data[radix] |= sub;

                        deltaAC += voxelSize * 0.5f;
                    }
                    deltaAB += voxelSize * 0.5f;
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
        if (!canDraw)
            return;


        // draw voxel grids
        Vector3 pos = Vector3.one * voxelSize * 0.5f;
        for (int x = 0; x < 32; ++x)
        {
            for (int y = 0; y < 16; ++y)
            {
                for (int z = 0; z < 32; ++z)
                {
                    Gizmos.color = new Color(0, 1, 0, 0.025f);
                    Gizmos.DrawWireCube(pos + new Vector3(x, y, z) * voxelSize, Vector3.one * voxelSize);
                }
            }
        }


        //draw sampling coordinate
        for (int i = 0; i < tempData.Count; ++i)
        {
            Vector3 center = tempData[i];
            Gizmos.color = new Color(0, 0, 1, 1f);  //blue
            Gizmos.DrawCube(center, Vector3.one * 0.025f);
            Gizmos.DrawWireCube(center, Vector3.one * 0.025f);
        }


        //draw voxel
        foreach (int radix in data.Keys)
        {
            float x = radix >> 16;
            float y = (radix & 0xFF00) >> 8;
            float z = radix & 0xFF;

            byte sub = data[radix];
            Vector3 center = new Vector3(x, y, z) * voxelSize + Vector3.one * voxelSize * 0.5f;

            // 복셀 자체는 잘 잡았는데;;
            Gizmos.color = new Color(0, 0, 1, 0.1f);
            Gizmos.DrawCube(center, Vector3.one * voxelSize);
            Gizmos.DrawWireCube(center, Vector3.one * voxelSize);
            //continue;

            Vector3 subCenter = center;
            for (int i = 0; i < 8; ++i)
            {
                bool isObstacle = (sub & (1 << i)) == 1;
                if (isObstacle)
                {
                    Gizmos.color = new Color(1, 0, 0, 0.5f); //red
                }
                else
                {
                    Gizmos.color = new Color(0f, 1, 0, 0); //???
                }

                float unit = voxelSize * 0.25f;
                switch (i)
                {
                    case 0: subCenter = center + new Vector3(-unit, -unit, -unit); break;
                    case 1: subCenter = center + new Vector3( unit, -unit, -unit); break;
                    case 2: subCenter = center + new Vector3(-unit, -unit,  unit); break;
                    case 3: subCenter = center + new Vector3( unit, -unit,  unit); break;

                    //case 4: subCenter = center + new Vector3(-unit, unit, -unit); break;
                    //case 5: subCenter = center + new Vector3(unit, unit, -unit); break;
                    //case 6: subCenter = center + new Vector3(-unit, unit, unit); break;
                    //case 7: subCenter = center + new Vector3(unit, unit, unit); break;
                }

                Gizmos.DrawCube(subCenter, Vector3.one * voxelSize * 0.5f);
                Gizmos.DrawWireCube(subCenter, Vector3.one * voxelSize * 0.5f);
            }
        }
    }
}