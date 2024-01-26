using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 줄 요약: NavMesh를 물리 기반이 아닌 Mesh 기반으로 만든다. (도대체 왜...)
/// </summary>
/// 

public enum VoxelState
{
    Plain = 0,
    Obstacle,
    Sloped,
}
public class MapSamplerSecond : MonoBehaviour
{
    [Header("File")]
    [SerializeField] private string fileName;

    [Header("Voxels")]
    [SerializeField] private Transform resourceTransform;
    [SerializeField] private float interval;

    private Dictionary<long, Voxel_t2> data;
    private MeshFilter[] meshFilters;

    private float unit = 0.25f;
    private float unit_invert;

    private int shift = 4 * 5; //4bytes * 5개 묶음
    private const long BITMASK_X = 0xFFFFF_00000_00000;
    private const long BITMASK_Y = 0x00000_FFFFF_00000;
    private const long BITMASK_Z = 0x00000_00000_FFFFF;

    private void Awake()
    {
        data = new Dictionary<long, Voxel_t2>();
        meshFilters = resourceTransform.GetComponentsInChildren<MeshFilter>();
        unit_invert = 1 / unit;
    }

    private System.Collections.IEnumerator IESampling()
    {
        Vector3 epsilon = Vector3.one * float.Epsilon;
        Vector3 A, B, C;
        VoxelState state;

        for (int f = 0; f < meshFilters.Length; ++f)
        {
            Transform targetTransform = meshFilters[f].transform;
            if (!targetTransform.gameObject.CompareTag("MapObject"))
            {
                continue;
            }

            Mesh mesh = meshFilters[f].mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            for (int t = 0; t < triangles.Length; t += 3)
            {
                // get status
                Vector3 normal1 = normals[triangles[t]];
                Vector3 normal2 = normals[triangles[t + 1]];
                Vector3 normal3 = normals[triangles[t + 2]];
                state = GetVoxelState(normal1, normal2, normal3);

                //sampling points
                A = targetTransform.TransformPoint(vertices[triangles[t]]) + epsilon;
                B = targetTransform.TransformPoint(vertices[triangles[t + 1]]) + epsilon;
                C = targetTransform.TransformPoint(vertices[triangles[t + 2]]) + epsilon;

                float distAB = Vector3.Distance(A, B);
                float interval = (distAB < unit) ? unit * 0.25f : unit;
                int samplingCountAB = Mathf.CeilToInt(distAB / interval);

                for (int ab = 0; ab < samplingCountAB; ++ab)
                {
                    float ratio = (float)ab / samplingCountAB;
                    Vector3 fromAB = Vector3.Lerp(A, B, ratio);
                    Vector3 toAC = Vector3.Lerp(A, C, ratio);

                    float distABtoAC = Vector3.Distance(fromAB, toAC);
                    interval = (distABtoAC < unit) ? unit * 0.25f : unit;
                    int samplingCountABtoAC = Mathf.CeilToInt(distABtoAC / interval);

                    for (int ac = 0; ac < samplingCountABtoAC; ++ac)
                    {
                        // get key
                        Vector3 samplingPoint = Vector3.Lerp(fromAB, toAC, (float)ac / samplingCountABtoAC);
                        long x = (long)(Mathf.CeilToInt(samplingPoint.x * unit_invert) * unit);
                        long y = (long)(Mathf.CeilToInt(samplingPoint.y * unit_invert) * unit);
                        long z = (long)(Mathf.CeilToInt(samplingPoint.z * unit_invert) * unit);
                        long key = (x << shift * 2) | (y << shift) | z;
                        
                        // add data
                        if (!data.ContainsKey(key))
                        {
                            data.Add(key, new Voxel_t2((int)state));
                        }
                        else if (data[key].State < state)
                        {
                            data[key] = new Voxel_t2((int)state);
                        }

                    }
                }

            }

            Debug.Log($"Sampling: {f} / {meshFilters.Length}");
            yield return null;
        }

        Debug.Log($"Sampling is over. (count: {data.Keys.Count})");
    }
    private void Sampling()
    {
        Vector3 epsilon = Vector3.one * float.Epsilon;
        Vector3 A, B, C;
        VoxelState state;
        float weight = unit * (1 / interval);

        for (int f = 0; f < meshFilters.Length; ++f)
        {
            Transform targetTransform = meshFilters[f].transform;
            if (!targetTransform.gameObject.CompareTag("MapObject"))
            {
                continue;
            }

            Mesh mesh = meshFilters[f].mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            for (int i = 0; i < vertices.Length; ++i)
            {
                Debug.Log($"[V:{i}] {targetTransform.TransformPoint(vertices[i])}");
                Debug.Log($"[N:{i}] {targetTransform.TransformDirection(normals[i])}");
            }
            for (int t = 0; t < triangles.Length; t += 3)
            {
                // get status
                Vector3 normal1 = normals[triangles[t]];
                Vector3 normal2 = normals[triangles[t + 1]];
                Vector3 normal3 = normals[triangles[t + 2]];
                state = GetVoxelState(normal1, normal2, normal3);

                //sampling points
                A = targetTransform.TransformPoint(vertices[triangles[t]]) + epsilon;
                B = targetTransform.TransformPoint(vertices[triangles[t + 1]]) + epsilon;
                C = targetTransform.TransformPoint(vertices[triangles[t + 2]]) + epsilon;

                float distAB = Vector3.Distance(A, B);
                float multiple = (distAB < unit) ? weight * 0.25f : weight;
                int samplingCountAB = (int)(distAB / (unit * multiple));

                for (int ab = 0; ab < samplingCountAB; ++ab)
                {
                    float ratio = (float)ab / samplingCountAB;
                    Vector3 fromAB = Vector3.Lerp(A, B, ratio);
                    Vector3 toAC = Vector3.Lerp(A, C, ratio);

                    float distABtoAC = Vector3.Distance(fromAB, toAC);
                    int samplingCountABtoAC = (int)(distABtoAC / (unit * multiple));

                    for (int ac = 0; ac < samplingCountABtoAC; ++ac)
                    {
                        // get key
                        Vector3 samplingPoint = Vector3.Lerp(fromAB, toAC, (float)ac / samplingCountABtoAC);
                        long x = Mathf.CeilToInt(samplingPoint.x * unit_invert);
                        long y = Mathf.CeilToInt(samplingPoint.y * unit_invert);
                        long z = Mathf.CeilToInt(samplingPoint.z * unit_invert);
                        long key = (x << shift * 2) | (y << shift) | z;

                        // add data
                        if (!data.ContainsKey(key))
                        {
                            data.Add(key, new Voxel_t2((int)state));
                        }
                        else if (data[key].State < state)
                        {
                            data[key] = new Voxel_t2((int)state);
                        }

                    }
                }
            }
        }

        Debug.Log($"Sampling is over. (count: {data.Keys.Count})");
    }


    private void Start()
    {
        if (fileName == string.Empty)
        {
            GameObject obj = resourceTransform.GetChild(0).gameObject;
            fileName = obj.name;
        }

        Coroutiner.PlayCoroutine(IESampling());
        //Sampling();
    }
    private VoxelState GetVoxelState(Vector3 n1, Vector3 n2, Vector3 n3)
    {
        VoxelState state = VoxelState.Plain;

        if (n1.y > 0 || n2.y > 0 || n3.y > 0)
        {
            state = VoxelState.Sloped;
        }
        if (n1.y < 0 || n2.y < 0 || n3.y < 0)
        {
            state = VoxelState.Obstacle;
        }

        return state;
    }
    private void OnDrawGizmos()
    {
        #region VOXEL
        //*
        if (data == null)
        {
            return;
        }
        foreach (var key in data.Keys)
        {
            long x = (key & BITMASK_X) >> shift * 2;
            long y = (key & BITMASK_Y) >> shift * 1;
            long z = (key & BITMASK_Z) >> shift * 0;
            Vector3 pivot = new Vector3(x, y, z);

            switch (data[key].State)
            {
                case VoxelState.Obstacle: Gizmos.color = new Color(1, 0, 0, 0.25f); break;
                case VoxelState.Sloped: Gizmos.color = new Color(0, 1, 0, 0.25f); break;
                default: continue;
            }
            Gizmos.DrawCube(pivot + Vector3.one * 0.5f * unit, Vector3.one * unit);

            Gizmos.color = Color.black;
            Gizmos.DrawCube(pivot + Vector3.one * 0.5f * unit, Vector3.one * 0.01f);
        }
        //*/
        #endregion

        #region normal 방향 체크
        /*
        if (meshFilters == null)
        {
            return;
        }
        for (int f = 0; f < meshFilters.Length; ++f)
        {
            Transform objTransform = meshFilters[f].transform;
            Mesh mesh = meshFilters[f].mesh;
            Vector3[] normals = mesh.normals;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            for (int t = 0; t < triangles.Length; ++t)
            {
                int index = triangles[t];
                Vector3 worldVertex = objTransform.TransformPoint(vertices[index]);
                Vector3 worldNormal = objTransform.TransformDirection(normals[index]);

                if (worldNormal.y > 0) { Gizmos.color = Color.green; }
                else if (worldNormal.y < 0) { Gizmos.color = Color.red; }
                else { Gizmos.color = Color.white; }

                Gizmos.DrawLine(worldVertex, worldVertex + worldNormal);
                Gizmos.DrawWireCube(worldVertex + worldNormal, Vector3.one * 0.1f);
            }
        }
        //*/
        #endregion
    }
}
