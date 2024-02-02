using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Public;
using PublicValue; //처음부터 이렇게 할 걸!

public class MapSampler3rd : MonoBehaviour
{
    [Header("File")]
    [SerializeField] private string fileName;

    [Header("Voxels")]
    [SerializeField] private Transform resourceTransform;
    [SerializeField] private float samplingInterval;
    [SerializeField] private Mesh gizmoMesh;
    [SerializeField] private float drawHeightLow;
    [SerializeField] private float drawHeightHigh;

    [SerializeField] private float[] gizmoAlpha;

    private Dictionary<int, Voxel_t2> data;
    private MeshFilter[] filter;

    private readonly float voxelDegree = 45f;
    private readonly float voxelDegreeDiv = 1f / 45f;

    private void Awake()
    {
        filter = resourceTransform.GetComponentsInChildren<MeshFilter>();
    }
    private void Start()
    {
        if (fileName == string.Empty)
        {
            GameObject obj = resourceTransform.GetChild(0).gameObject;
            fileName = obj.name;
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            //DataTable.WriteBinaryMapVoxel(data, fileName);
            Debug.Log("save");
        }
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (data != null)
            {
                data.Clear();
            }
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            //data = DataTable.LoadMapVoxel(fileName);
            Sampling();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (data != null)
            {
                data.Clear();
            }

            filter = resourceTransform.GetComponentsInChildren<MeshFilter>();
            Sampling();
        }
    }
    private void  Sampling()
    {
        Coroutiner.PlayCoroutine(SamplingVoxels());
    }
    private IEnumerator SamplingVoxels()
    {
        data = new Dictionary<int, Voxel_t2>();

        SubVoxelType subVoxelType;
        Vector3 A, B, C;

        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            Quaternion rotation = targetTransform.rotation;

            Mesh mesh = filter[f].mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            int objType;
            if      (targetTransform.CompareTag("Plain"))       { objType = 1; }
            else if (targetTransform.CompareTag("Obstacle"))    { objType = 2; }
            else if (targetTransform.CompareTag("Slope"))       { objType = 3; }
            else    { objType = 0; }

            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 normal1 = rotation * normals[triangles[t]];
                normal1.Normalize();
                Vector3 normal2 = rotation * normals[triangles[t + 1]];
                normal2.Normalize();
                Vector3 normal3 = rotation * normals[triangles[t + 2]];
                normal3.Normalize();

                subVoxelType = GetSubVoxelType(normal1.y, normal2.y, normal3.y);
                if (subVoxelType == SubVoxelType.None)
                    continue;

                A = targetTransform.TransformPoint(vertices[triangles[t]]);
                B = targetTransform.TransformPoint(vertices[triangles[t + 1]]);
                C = targetTransform.TransformPoint(vertices[triangles[t + 2]]);

                float distAB = Vector3.Distance(A, B);
                float interval = (VOXEL_SIZE > distAB) ? samplingInterval * 4f : samplingInterval;
                int samplingCountAB = Mathf.FloorToInt(distAB / VOXEL_HALF_SIZE * interval);

                for (int i = 0; i < samplingCountAB; ++i)
                {
                    float ratio = (float)i / samplingCountAB;
                    Vector3 AB = Vector3.Lerp(A, B, ratio);
                    Vector3 AC = Vector3.Lerp(A, C, ratio);

                    float distABtoAC = Vector3.Distance(AB, AC);
                    int samplingCountABtoAC = Mathf.FloorToInt(distABtoAC / VOXEL_HALF_SIZE * interval) - 1;

                    for (int j = 0; j < samplingCountABtoAC; ++j)
                    {
                        Vector3 samplingPoint = Vector3.Lerp(AB, AC, (float)j / samplingCountABtoAC);
                        SetVoxel(samplingPoint, objType, subVoxelType);
                    }
                }
            }

            Debug.Log($"Now Sampling ({f + 1}/{filter.Length})");
            yield return null;
        }

        Debug.Log($"Sampling Done. (count:{data.Keys.Count})");
    }

    private SubVoxelType GetSubVoxelType(float y1, float y2, float y3)
    {
        float[] y = new float[] { y1, y2, y3 };

        //가장 작은 y(normal.y)를 찾기
        for (int i = 0; i < 3 - 1; ++i)
        {
            for (int j = 0; j < 3 - i - 1; ++j)
            {
                if (y[j] > y[j + 1])
                {
                    float temp = y[j];
                    y[j] = y[j + 1];
                    y[j + 1] = temp;
                }
            }
        }

        //y 최소값에 따라 타입 분류
        int min = Mathf.FloorToInt(y[0] * 1000f);
        SubVoxelType type;
        if      (min == -1000)          { type = SubVoxelType.Obstacle; }
        else if (min == 1000)           { type = SubVoxelType.Plain; }
        else if (0 < min && min < 1000) { type = SubVoxelType.Slope45; }
        else                            { type = SubVoxelType.None; }

        return type;
    }
    private void SetVoxel(Vector3 point, int objectType, SubVoxelType subVoxelType)
    {
        //부동소수점 문제를 피하고자 소수점 3자리까지만
        float x = Mathf.CeilToInt(point.x * 1000f) * 0.001f;
        float y = Mathf.CeilToInt(point.y * 1000f) * 0.001f;
        float z = Mathf.CeilToInt(point.z * 1000f) * 0.001f;
        point = new Vector3(x, y, z);

        Vector3 center = GetCenter(point);
        int index = Parser.GetVoxelIndex(center);

        int shift = GetMovableIndex(point - center);
        if (shift == -1)
        {
            //경계선은 애매하니 체크하지 않는다.
            return;
        }

        int movable = 0;
        switch (subVoxelType)
        {
            case SubVoxelType.Plain:     movable = 1 << shift;  break;
            case SubVoxelType.Obstacle:  movable = 0;  break;
            case SubVoxelType.Slope45:   movable = 1 << shift;  break;
        }

        if (!data.TryGetValue(index, out Voxel_t2 voxel))
        {
            data.Add(index, new Voxel_t2(objectType, (int)subVoxelType, movable));
            return;
        }

        if (objectType >= (int)voxel.ObjectType)
        {
            if (subVoxelType == SubVoxelType.Obstacle)
            {
                movable = voxel.Move & ~(1 << shift);
            }
            else
            {
                movable = voxel.Move | (1 << shift);
            }

            if (subVoxelType != SubVoxelType.Plain)
            {
                Debug.Log($"[{subVoxelType}] {System.Convert.ToString(voxel.Move, 2)} << {shift}");
            }

            data[index] = new Voxel_t2(objectType, (int)subVoxelType, movable);
        }
    }
    private Vector3 GetCenter(Vector3 point)
    {
        float cx = Mathf.Floor(point.x * VOXEL_HALF_INVERT) * VOXEL_HALF_SIZE;
        float cy = Mathf.Floor(point.y * VOXEL_HALF_INVERT) * VOXEL_HALF_SIZE;
        //float cy = Mathf.Floor(point.y * VOXEL_INVERT * 2f + 1) * VOXEL_HALF_SIZE;
        float cz = Mathf.Floor(point.z * VOXEL_HALF_INVERT) * VOXEL_HALF_SIZE;

        Vector3 center;
        Vector3 p1, p2;

        if ((cz - cx) % VOXEL_SIZE == 0)
        {
            p1 = new Vector3(cx, cy, cz);                                      // half-size clamp
            p2 = new Vector3(cx + VOXEL_HALF_SIZE, cy, cz + VOXEL_HALF_SIZE);  // half-size clamp + new Vector3(1,0,1);
        }
        else
        {
            p1 = new Vector3(cx + VOXEL_HALF_SIZE, cy, cz);   // half-size clamp + Vector3.right
            p2 = new Vector3(cx, cy, cz + VOXEL_HALF_SIZE);   // half-size clamp + Vector3.up
        }

        if (Vector3.Distance(point, p1) <= Vector3.Distance(point, p2))
        {
            center = p1;
        }
        else
        {
            center = p2;
        }

        cx = (center.x < 0f) ? 0f : center.x;
        cy = (center.y < 0f) ? VOXEL_HALF_SIZE : center.y;
        cz = (center.z < 0f) ? 0f : center.z;

        return new Vector3(cx, cy, cz);
    }
    private int GetMovableIndex(Vector3 diff)
    {
        float angle = Mathf.Atan2(diff.z, diff.x) * Mathf.Rad2Deg;
        angle = Mathf.Floor((angle + 360) % 360);

        if (Mathf.Approximately(angle % voxelDegree, 0))
        {
            return -1;
        }

        return (int)(angle * voxelDegreeDiv);
    }

    private void OnDrawGizmos()
    {
        if (data == null)
        {
            return;
        }

        float gizmoSize = VOXEL_HALF_SIZE * 0.5f / Mathf.Sqrt(2);

        //draw voxel
        foreach (int index in data.Keys)
        {
            float x = index >> 16;
            float y = (index & 0xFF00) >> 8;
            float z = index & 0xFF;

            Vector3 center = new Vector3(x, y, z) * VOXEL_HALF_SIZE;

            if (center.y < drawHeightLow || center.y > drawHeightHigh)
                continue;

            bool IsContoured = false;
            for (int i = 0; i < 8; ++i)
            {
                if (data[index].IsMovable(i))
                {
                    Gizmos.color = new Color(0, 1, 0, gizmoAlpha[0]);
                }
                else
                {
                    Gizmos.color = new Color(1, 0, 0, gizmoAlpha[1]);
                }

                Quaternion gizmoRot = Quaternion.identity;
                float voxelQuater = VOXEL_HALF_SIZE * 0.5f;
                Vector3 gizmoPos = center + Vector3.up * 0.125f; //관상용으로 up
                switch (i)
                {
                    case 0:
                        gizmoRot = Quaternion.Euler(0, 225f, 90f);
                        gizmoPos += new Vector3(voxelQuater, 0f, 0f);
                        break;
                    case 1:
                        gizmoRot = Quaternion.Euler(0, 315f, 90f);
                        gizmoPos += new Vector3(0f, 0f, voxelQuater);
                        break;
                    case 2:
                        gizmoRot = Quaternion.Euler(0, 135f, 90f);
                        gizmoPos += new Vector3(0f, 0f, voxelQuater);
                        break;
                    case 3:
                        gizmoRot = Quaternion.Euler(0, 225f, 90f);
                        gizmoPos += new Vector3(-voxelQuater, 0f, 0f);
                        break;
                    case 4:
                        gizmoRot = Quaternion.Euler(0, 45f, 90f);
                        gizmoPos += new Vector3(-voxelQuater, 0f, 0f);
                        break;
                    case 5:
                        gizmoRot = Quaternion.Euler(0, 135f, 90f);
                        gizmoPos += new Vector3(0f, 0f, -voxelQuater);
                        break;
                    case 6:
                        gizmoRot = Quaternion.Euler(0, 315f, 90f);
                        gizmoPos += new Vector3(0f, 0f, -voxelQuater);
                        break;
                    case 7:
                        gizmoRot = Quaternion.Euler(0, 45f, 90f);
                        gizmoPos += new Vector3(voxelQuater, 0f, 0f);
                        break;
                }

                Gizmos.matrix = Matrix4x4.TRS(gizmoPos, gizmoRot, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                Gizmos.DrawMesh(gizmoMesh, Vector3.zero, Quaternion.identity);
                Gizmos.matrix = Matrix4x4.identity;

                if (!IsContoured)
                {
                    Gizmos.color = new Color(1f, 0.922f, 0.016f, gizmoAlpha[2]);
                    Gizmos.DrawLine(center + Vector3.right * VOXEL_HALF_SIZE, center + Vector3.back * VOXEL_HALF_SIZE);
                    Gizmos.DrawLine(center + Vector3.back * VOXEL_HALF_SIZE, center + Vector3.left * VOXEL_HALF_SIZE);
                    Gizmos.DrawLine(center + Vector3.left * VOXEL_HALF_SIZE, center + Vector3.forward * VOXEL_HALF_SIZE);
                    Gizmos.DrawLine(center + Vector3.forward * VOXEL_HALF_SIZE, center + Vector3.right * VOXEL_HALF_SIZE);

                    Gizmos.color = new Color(0f, 0f, 1f, gizmoAlpha[3]);
                    Gizmos.DrawLine(center + Vector3.left * VOXEL_HALF_SIZE, center + Vector3.right * VOXEL_HALF_SIZE);
                    Gizmos.DrawLine(center + Vector3.forward * VOXEL_HALF_SIZE, center + Vector3.back * VOXEL_HALF_SIZE);
                    Gizmos.DrawLine(center + new Vector3(-1, 0, -1) * voxelQuater, center + new Vector3(1, 0, 1) * voxelQuater);
                    Gizmos.DrawLine(center + new Vector3(-1, 0, 1) * voxelQuater, center + new Vector3(1, 0, -1) * voxelQuater);

                    IsContoured = true;
                }
            }
        }
    }
}
