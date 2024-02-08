using CMathf;
using PublicValue;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Public;

public class MapSampler4th : MonoBehaviour
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

    private Dictionary<int, Voxel_t4> data;
    private MeshFilter[] filter;

    private readonly float voxelDegree = 45f;
    private readonly float voxelDegreeDiv = 1f / 45f;

    List<Vector3> c = new List<Vector3>();

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

        Sampling();
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
    private void Sampling()
    {
        Coroutiner.PlayCoroutine(SamplingVoxels());
    }
    private IEnumerator SamplingVoxels()
    {
        data = new Dictionary<int, Voxel_t4>();

        Vector3 A, B, C;

        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            Quaternion rotation = targetTransform.rotation;

            Mesh mesh = filter[f].mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 normal1 = rotation * normals[triangles[t]];
                normal1.Normalize();
                Vector3 normal2 = rotation * normals[triangles[t + 1]];
                normal2.Normalize();
                Vector3 normal3 = rotation * normals[triangles[t + 2]];
                normal3.Normalize();

                int degreeInt = GetDegreeInt(normal1.y, normal2.y, normal3.y);
                if (degreeInt == -1)
                {
                    continue;
                }

                A = targetTransform.TransformPoint(vertices[triangles[t]]);
                B = targetTransform.TransformPoint(vertices[triangles[t + 1]]);
                C = targetTransform.TransformPoint(vertices[triangles[t + 2]]);

                float distAB = Vector3.Distance(A, B);
                float interval = (VOXEL_SIZE > distAB) ? samplingInterval * 4f : samplingInterval;
                int samplingCountAB = CMathf.CMath.FloorToInt1000(distAB / VOXEL_HALF_SIZE * interval);

                for (int i = 1; i < samplingCountAB - 1; ++i)
                {
                    float ratio = CMath.Floor1000((float)i / samplingCountAB);
                    Vector3 AB = Vector3.Lerp(A, B, ratio);
                    Vector3 AC = Vector3.Lerp(A, C, ratio);

                    float distABtoAC = Vector3.Distance(AB, AC);
                    int samplingCountABtoAC = CMath.FloorToInt1000(distABtoAC / VOXEL_HALF_SIZE * interval);

                    for (int j = 1; j < samplingCountABtoAC - 1; ++j)
                    {
                        ratio = CMath.Floor1000((float)j / samplingCountABtoAC);
                        Vector3 samplingPoint = Vector3.Lerp(AB, AC, ratio);
                        SetVoxel(samplingPoint, degreeInt);
                    }
                }
            }

            Debug.Log($"Now Sampling ({f + 1}/{filter.Length})");
            yield return null;
        }

        Debug.Log($"Sampling count:({data.Keys.Count})");

    }

    private int GetDegreeInt(float y1, float y2, float y3)
    {
        float min = y1;
        if (y2 < min) { min = y2; }
        if (y3 < min) { min = y3; }

        min = CMath.Floor1000(min);

        if (min == -1.000f) { return 90; }
        else
        if (min ==  1.000f) { return 0; }
        else
        if (min ==  0.707f) { return 45; }
        else
        if (min ==  0.500f) { return 30; } 

        return -1;
    }
    private void SetVoxel(Vector3 point, int degreeInt)
    {
        float x = CMath.Ceil1000(point.x);
        float y = CMath.Ceil1000(point.y);
        float z = CMath.Ceil1000(point.z);

        point = new Vector3(x, y, z);
        Vector3 center = GetCenter(point);

        int idxVoxel = Parser.GetVoxelIndex(center);
        int idxSub = GetMovableIndex(point - center);
        if (idxSub == -1)
        {
            return;
        }

        int moveType = GetDegreeType(degreeInt);
        if (!data.TryGetValue(idxVoxel, out Voxel_t4 voxel))
        {
            int bit = (moveType > 0) ? 1 : 0;
            data.Add(idxVoxel, new Voxel_t4(idxSub, degreeInt, bit << idxSub));
            c.Add(center);
        }
        else if (moveType >= GetDegreeType(voxel.BitToDeg(idxSub)))
        {
            int degreeMask = voxel.GetDegreeMask(idxSub, degreeInt);

            int bit = (moveType != 1) ? 1 : 0;
            int moveMask = voxel.GetMoveMask(idxSub, bit);
            data[idxVoxel] = new Voxel_t4(degreeMask | moveMask);
        }
    }
    private int GetDegreeType(int degreeInt)
    {
        int type = 0;
        switch (degreeInt)
        {
            case 0:
                type = 0;
                break;
            case 90:
                type = 1;
                break;
            case 30:
            case 45:
                type = 2;
                break;
        }

        return type;
    }
    private Vector3 GetCenter(Vector3 point)
    {
        float cx = Mathf.Floor(point.x * VOXEL_HALF_INVERT) * VOXEL_HALF_SIZE;
        float cy = Mathf.Floor(point.y * VOXEL_HALF_INVERT) * VOXEL_HALF_SIZE;
        float cz = Mathf.Floor(point.z * VOXEL_HALF_INVERT) * VOXEL_HALF_SIZE;

        //값이 너무 작아서 소수점 3자리로 처리가 안된다 ㅅㄱ;
        //float cx = CMath.Floor1000(point.x * VOXEL_HALF_INVERT) * VOXEL_HALF_SIZE;
        //float cy = CMath.Floor1000(point.y * VOXEL_HALF_INVERT) * VOXEL_HALF_SIZE;
        //float cz = CMath.Floor1000(point.z * VOXEL_HALF_INVERT) * VOXEL_HALF_SIZE;

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

        Gizmos.color = Color.black;
        for (int i = 0; i < c.Count; ++i)
        {
            Gizmos.DrawCube(c[i], Vector3.one * 0.025f);
        }

        //draw voxel
        float gizmoSize = VOXEL_HALF_SIZE * 0.5f / Mathf.Sqrt(2);
        foreach (int index in data.Keys)
        {
            float x = index >> 16;
            float y = (index & 0xFF00) >> 8;
            float z = index & 0xFF;

            Vector3 center = new Vector3(x, y, z) * VOXEL_HALF_SIZE;
            if (center.y < drawHeightLow || center.y > drawHeightHigh)
                continue;

            Voxel_t4 voxel = data[index];
            bool IsContoured = false;
            for (int i = 0; i < 8; ++i)
            {
                if (voxel.IsMovable(i))
                {
                    if (voxel.BitToDeg(i) == 0)
                    {
                        Gizmos.color = new Color(0, 1, 0, gizmoAlpha[0]); //green
                    }
                    else
                    {
                        Gizmos.color = new Color(0, 0, 1, gizmoAlpha[0]); //blue
                    }
                }
                else
                {
                    Gizmos.color = new Color(1, 0, 0, gizmoAlpha[1]); //red
                }

                Quaternion gizmoRot = Quaternion.identity;
                float voxelQuater = VOXEL_HALF_SIZE * 0.5f;
                Vector3 gizmoPos = center + Vector3.up * 0.125f; // size 1/8 up
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
