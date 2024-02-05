using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Public;
using PublicValue;
using CMathf;
using System.Xml;

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
    private Dictionary<int, byte> valid = new Dictionary<int, byte>();

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

        VoxelType subVoxelType;
        Vector3 A, B, C;

        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            Quaternion rotation = targetTransform.rotation;

            Mesh mesh = filter[f].mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            VoxelType objType; //obj != voxel. 타입 분류 재정의 필요.
            if      (targetTransform.CompareTag("Plain"))       { objType = VoxelType.Plain; }
            else if (targetTransform.CompareTag("Slope"))       { objType = VoxelType.Slope45; }
            else if (targetTransform.CompareTag("Obstacle"))    { objType = VoxelType.Obstacle; }
            else    { objType = VoxelType.None; }

            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 normal1 = rotation * normals[triangles[t]];
                normal1.Normalize();
                Vector3 normal2 = rotation * normals[triangles[t + 1]];
                normal2.Normalize();
                Vector3 normal3 = rotation * normals[triangles[t + 2]];
                normal3.Normalize();

                subVoxelType = GetSubVoxelType(normal1.y, normal2.y, normal3.y);
                if (subVoxelType == VoxelType.None)
                    continue;

                A = targetTransform.TransformPoint(vertices[triangles[t]]);
                B = targetTransform.TransformPoint(vertices[triangles[t + 1]]);
                C = targetTransform.TransformPoint(vertices[triangles[t + 2]]);

                float distAB = Vector3.Distance(A, B);
                float interval = (VOXEL_SIZE > distAB) ? samplingInterval * 4f : samplingInterval;
                int samplingCountAB = Mathf.FloorToInt(distAB / VOXEL_HALF_SIZE * interval);

                for (int i = 1; i < samplingCountAB-1; ++i)
                {
                    float ratio = CMath.FloorToInt1000((float)i / samplingCountAB);
                    Vector3 AB = Vector3.Lerp(A, B, ratio);
                    Vector3 AC = Vector3.Lerp(A, C, ratio);

                    float distABtoAC = Vector3.Distance(AB, AC);
                    int samplingCountABtoAC = Mathf.FloorToInt(distABtoAC / VOXEL_HALF_SIZE * interval);

                    for (int j = 1; j < samplingCountABtoAC - 1; ++j)
                    {
                        ratio = CMath.FloorToInt1000((float)j / samplingCountABtoAC);
                        Vector3 samplingPoint = Vector3.Lerp(AB, AC, ratio);
                        SetVoxel(samplingPoint, objType, subVoxelType);
                    }
                }
            }

            Debug.Log($"Now Sampling ({f + 1}/{filter.Length})");
            yield return null;
        }

        List<int> keys = new List<int>(data.Keys);

        PostProcessObstacle(keys);
        Debug.Log($"Sampling Done. (count:{data.Keys.Count})");
    }
    private void PostProcessObstacle(List<int> keys)
    {
        List<int> notyet = new List<int>();
        
        //1.
        for (int i = 0; i < keys.Count; ++i)
        {
            int index = keys[i];
            Voxel_t2 voxel = data[index];
            int erase = 0;

            if (voxel.ObjectType == VoxelType.Obstacle)
            {
                if (voxel.Move == 0xFF)
                {
                    int halfInt = (int)VOXEL_HALF_INVERT;
                    int idxNeighbor;

                    //left
                    idxNeighbor = index - (halfInt << 16);
                    if (!data.TryGetValue(idxNeighbor, out Voxel_t2 neighbor)
                        || (neighbor.Move & 0b_1000_0001) == 0)
                    {
                        erase |= 0b_0011_1100;
                    }

                    //right
                    idxNeighbor = index + (halfInt << 16);
                    if (!data.TryGetValue(idxNeighbor, out neighbor)
                        || (neighbor.Move & 0b_0001_1000) == 0)
                    {
                        erase |= 0b_1100_0011;
                    }

                    //up
                    idxNeighbor = index + halfInt;
                    if (!data.TryGetValue(idxNeighbor, out neighbor)
                        || (neighbor.Move & 0b_0110_0000) == 0)
                    {
                        erase |= 0b_0000_1111;
                    }

                    //down
                    idxNeighbor = index - halfInt;
                    if (!data.TryGetValue(idxNeighbor, out neighbor)
                        || (neighbor.Move & 0b_0000_0110) == 0)
                    {
                        erase |= 0b_1111_0000;
                    }

                    if (erase != 0x00)
                    {
                        erase = voxel.Data & ~(erase);
                        data[index] = new Voxel_t2(erase);
                    }
                    else
                    {
                        notyet.Add(index);
                    }
                }
                else
                {
                    notyet.Add(index);
                }
            }
        }

        //2.
        for (int i = 0; i < notyet.Count; ++i)
        {
            int index = notyet[i];
            Voxel_t2 voxel = data[index];

            if (voxel.Move != 0x00 && voxel.Move != 0xFF)
            {
                data[index] = new Voxel_t2(voxel.Data & ~(0xFF));
            }
        }
    }
    private void PostProcessSlope()
    { 
        
    }

    private VoxelType GetSubVoxelType(float y1, float y2, float y3)
    {
        float[] y = new float[] { y1, y2, y3 };

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

        int min = Mathf.FloorToInt(y[0] * 1000f);
        VoxelType type;
        if      (min == -1000)          { type = VoxelType.Obstacle; }
        else if (min == 1000)           { type = VoxelType.Plain; }
        else if (0 < min && min < 1000) { type = VoxelType.Slope45; }
        else                            { type = VoxelType.None; }

        return type;
    }
    private void SetVoxel(Vector3 point, VoxelType objectType, VoxelType targetType)
    {
        float x = CMath.CeilToInt1000(point.x);
        float y = CMath.CeilToInt1000(point.y);
        float z = CMath.CeilToInt1000(point.z);

        point = new Vector3(x, y, z);

        Vector3 center = GetCenter(point);
        int index = Parser.GetVoxelIndex(center);

        int shift = GetMovableIndex(point - center);
        if (shift == -1)
        {
            return;
        }

        int movable = 0;
        switch (targetType)
        {
            case VoxelType.Plain:       movable = 1 << shift;  break;
            case VoxelType.Slope45:     movable = 1 << shift; break;
            case VoxelType.Obstacle:    movable = 0;  break;
        }

        if (!data.TryGetValue(index, out Voxel_t2 voxel))
        {
            data.Add(index, new Voxel_t2(objectType, (int)targetType, movable));
            return;
        }

        if (targetType == VoxelType.Obstacle)
        {
            movable = voxel.Move & ~(1 << shift);
        }
        else
        {
            movable = voxel.Move | (1 << shift);
        }

        data[index] = new Voxel_t2(objectType, (int)targetType, movable);

        if (!valid.ContainsKey(index))
        {
            valid.Add(index, (byte)(1 << shift));
        }
        else
        {
            valid[index] |= (byte)(1 << shift);
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

        Gizmos.color = Color.black;
        foreach (var vIndex in valid.Keys)
        {
            float vx = vIndex >> 16;
            float vy = (vIndex & 0xFF00) >> 8;
            float vz = vIndex & 0xFF;

            Gizmos.DrawCube(new Vector3(vx, vy, vz) * VOXEL_HALF_SIZE, Vector3.one * 0.025f);
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
