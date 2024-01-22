using System.Collections.Generic;
using System.ComponentModel;
using Unity.Burst.Intrinsics;
using UnityEngine;

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
        float grid_invert = 1 / grid;
        float halfGrid = grid * 0.5f;
        float halfGrid_invert = 1 / halfGrid;

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
                        //sampling point
                        Vector3 samplingPoint = Vector3.Lerp(fromAB, toAC, (float)j / samplingCountABtoAC);

                        //find center point
                        float cx = Mathf.Floor(samplingPoint.x * halfGrid_invert) * halfGrid;
                        float cy = Mathf.Floor(samplingPoint.y * grid_invert + 1) * halfGrid;
                        float cz = Mathf.Floor(samplingPoint.z * halfGrid_invert) * halfGrid;

                        Vector3 center;
                        Vector3 comp1, comp2;
                        if ((cz - cx) % grid == 0)
                        {
                            comp1 = new Vector3(cx,            cy, cz           ); // half-size clamp
                            comp2 = new Vector3(cx + halfGrid, cy, cz + halfGrid); // half-size clamp + new Vector3(1,0,1);
                        }
                        else
                        {
                            comp1 = new Vector3(cx + halfGrid, cy, cz           ); // half-size clamp + Vector3.right
                            comp2 = new Vector3(cx           , cy, cz + halfGrid); // half-size clamp + Vector3.up
                        }

                        if (Vector3.Distance(samplingPoint, comp1) <= Vector3.Distance(samplingPoint, comp2))
                        {
                            center = comp1;
                        }
                        else
                        {
                            center = comp2;
                        }

                        //get radix
                        int radix = (int)(center.x * halfGrid_invert)   << 16
                                    | (int)(center.y * halfGrid_invert) << 8
                                    | (int)(center.z * halfGrid_invert) << 0;

                        if (!data.ContainsKey(radix))
                        {
                            data.Add(radix, new Voxel_t(type, 0x0000));
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

            Gizmos.color = new Color(0, 1, 0, 0.25f);
            Gizmos.DrawLine(Vector3.zero, Vector3.right * 100);
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * 100);

            Vector3 start;
            for (int i = 0; i < 100; ++i)
            {
                for (int j = 0; j < 100; ++j)
                {
                    start = new Vector3(i, 0, j) * grid;

                    Vector3 left = start + Vector3.forward * halfGrid;
                    Gizmos.DrawLine(left, left + new Vector3(1,0,1) * halfGrid);
                    Gizmos.DrawLine(left, left + new Vector3(1, 0, -1) * halfGrid);

                    Vector3 right = start + new Vector3(grid, 0, halfGrid);
                    Gizmos.DrawLine(right, right + new Vector3(-1, 0, 1) * halfGrid);
                    Gizmos.DrawLine(right, right + new Vector3(-1, 0, -1) * halfGrid);
                }
            }

            //draw voxel
            foreach (int radix in data.Keys)
            {
                float x = radix >> 16;
                float y = (radix & 0xFF00) >> 8;
                float z = radix & 0xFF;

                Vector3 center = new Vector3(x, y, z) * halfGrid;

                Gizmos.color = Color.black;
                Gizmos.DrawCube(center, Vector3.one * 0.025f);

                //for (int i = 0; i < 8; ++i)
                //{
                //    Vector3 dir = Vector3.zero;
                //    float quaterSize = halfSize * 0.5f;
                //    switch (i)
                //    {
                //        case 0: dir = new Vector3(quaterSize, -quaterSize, quaterSize); break;
                //        case 1: dir = new Vector3(-quaterSize, -quaterSize, quaterSize); break;
                //        case 2: dir = new Vector3(-quaterSize, -quaterSize, -quaterSize); break;
                //        case 3: dir = new Vector3(quaterSize, -quaterSize, -quaterSize); break;
                //        case 4: dir = new Vector3(quaterSize, quaterSize, quaterSize); break;
                //        case 5: dir = new Vector3(-quaterSize, quaterSize, quaterSize); break;
                //        case 6: dir = new Vector3(-quaterSize, quaterSize, -quaterSize); break;
                //        case 7: dir = new Vector3(quaterSize, quaterSize, -quaterSize); break;
                //    }

                //    Vector3 subCenter = center + dir;
                //    int sub_type = (data[radix].Sub & (0b11 << i * 2)) >> i * 2;
                //    switch ((VoxelType)sub_type)
                //    {
                //        // case VoxelType.None: continue;
                //        case VoxelType.Movable:
                //            Gizmos.color = Color.green;
                //            Gizmos.DrawCube(subCenter, Vector3.one * 0.025f);
                //            break;
                //        case VoxelType.Obstacle:
                //            Gizmos.color = Color.red;
                //            Gizmos.DrawCube(subCenter, Vector3.one * 0.025f);
                //            break;
                //        case VoxelType.None:
                //            Gizmos.color = new Color(1, 1, 1, 0f);
                //            break;
                //    }
                //    Gizmos.DrawLine(center, subCenter);

                //}
            }
        }
    }
}