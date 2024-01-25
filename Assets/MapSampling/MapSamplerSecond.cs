using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 줄 요약: NavMesh를 물리 기반이 아닌 Mesh 기반으로 만든다. (도대체 왜...)
/// </summary>
/// 

public enum VoxelState
{ 
    None = 0,
    Sloped = 1,
    //2
    Obstacle = 3
}
public class MapSamplerSecond : MonoBehaviour
{
    [Header("File")]
    [SerializeField] private string fileName;

    [Header("Voxels")]
    [SerializeField] private Transform resourceTransform;

    private Dictionary<long, Voxel_t> data;
    private MeshFilter[] filter;

    private float unit = 0.01f;
    private float unit_invert;

    private void Awake()
    {
        data = new Dictionary<long, Voxel_t>();
        filter = resourceTransform.GetComponentsInChildren<MeshFilter>();
        unit_invert = 1 / unit;
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
    private void Sampling()
    {
        Vector3 epsilon = Vector3.one * float.Epsilon;
        Vector3 A, B, C;
        VoxelState status; //0:평지, 1:기울음 2:?? 3:이동불가

        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            Mesh mesh = filter[f].mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            //얘도 걍 법선 방향으로 조지면 되는거 아니오?
            //targetTransform = filter[f].transform;
            //if (targetTransform.CompareTag("Movable")) { type = VoxelType.Movable; }
            //else if (targetTransform.CompareTag("Obstacle")) { type = VoxelType.Obstacle; }
            //else { type = VoxelType.None; }

            for (int t = 0; t < triangles.Length;)
            {
                A = targetTransform.TransformPoint(vertices[triangles[t++]]) + epsilon;
                B = targetTransform.TransformPoint(vertices[triangles[t++]]) + epsilon;
                C = targetTransform.TransformPoint(vertices[triangles[t++]]) + epsilon;

                float distAB = Vector3.Distance(A, B);
                int samplingCountAB = Mathf.CeilToInt(distAB / unit);

                //여기서부터 노멀을 여차저차 하는 것인디...
                //그리고..높낮이를 찍어봐야겠는디...
                Vector3 l1 = normals[triangles[t - 3]];
                Vector3 l2 = normals[triangles[t - 2]];
                Vector3 l3 = normals[triangles[t - 1]];

                //y가 하나라도 아래로 내려가면 obstacle로 처리?
                //이거 경우 나눠서 해야겠구먼
                if (l1.y < 0 || l2.y < 0 || l3.y < 0) { status = VoxelState.Obstacle; }
                else if (l1.y > 0 || l2.y > 0 || l3.y > 0) { status = VoxelState.Sloped; }
                else { status = VoxelState.None; }

                for (int ab = 0; ab < samplingCountAB; ++ab)
                {
                    float ratio = (float)ab / samplingCountAB;
                    Vector3 fromAB = Vector3.Lerp(A, B, ratio);
                    Vector3 toAC = Vector3.Lerp(A, C, ratio);

                    float distABtoAC = Vector3.Distance(fromAB, toAC);
                    int samplingCountABtoAC = Mathf.CeilToInt(distABtoAC / unit);

                    for (int ac = 0; ac < samplingCountABtoAC; ++ac)
                    {
                        Vector3 samplingPoint = Vector3.Lerp(fromAB, toAC, (float)ac / samplingCountABtoAC);

                        //ceil이 맞으려나? 그냥 넣을까 했으나 부동소수점 이슈..
                        
                        int x = Mathf.CeilToInt(samplingPoint.x * unit_invert);
                        int y = Mathf.CeilToInt(samplingPoint.y * unit_invert);
                        int z = Mathf.CeilToInt(samplingPoint.z * unit_invert);

                        //검사 방식으로 곱해버릴까.
                        long key = (x << 40) | (y << 20) | z;
                        if (!data.ContainsKey(key))
                        {
                            data.Add(key, new Voxel_t((int)status));
                            Debug.Log($"[{key}] {System.Convert.ToString(z,2)}");
                        }
                        else if (data[key].SubVoxel < (int)status)
                        {
                            data[key] = new Voxel_t((int)status);
                        }

                    }
                }
            }
        }

        Debug.Log($"Sampling is over. (count:{data.Keys.Count})");

        foreach (long key in data.Keys) //뭔가 밀림 현상 비슷한게 있나..? 이상한데;;;
        {
            long x = (key >> 40) & 0xFFFFF;
            long y = (key >> 20) & 0xFFFFF;
            long z = (key >>  0) & 0xFFFFF;

            //Vector3 p = new Vector3(x, y, z) * unit;
            Debug.Log($"[{key}] {System.Convert.ToString(z, 2)}");
            //Debug.Log($"[{key}] {x*unit}, {y * unit}, {z * unit}");
        }
    }

    private void OnDrawGizmos()
    {
        //테스트
        if (data == null || data.Keys.Count <= 0)
        {
            return;
        }
    }
}
