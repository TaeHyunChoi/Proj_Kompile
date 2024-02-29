using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CDataStructure;
using CMathf;
using static Public;

public class MapSampling7th : MonoBehaviour
{
    [SerializeField] private Transform resourceTransform;

    private Dictionary<int, Voxel_t2> map;
    private MeshFilter[] filter;

    private void Awake()
    {
        filter = resourceTransform.GetComponentsInChildren<MeshFilter>();
        map = new Dictionary<int, Voxel_t2>();
    }
    private void Start()
    {
        for (int f = 0; f < filter.Length; ++f)
        {
            Transform targetTransform = filter[f].transform;
            Mesh mesh = filter[f].mesh;

            Quaternion rot      = targetTransform.rotation;
            Vector3[] vertices  = mesh.vertices;
            Vector3[] normals   = mesh.normals;
            int[] triangles     = mesh.triangles;

            for (int t = 0; t < triangles.Length; t += 3)
            {
                int t0 = triangles[t];
                int t1 = triangles[t + 1];
                int t2 = triangles[t + 2];

                //normal <= 0 이면 할 필요도 없다?
                if (false == IsValidTriangle(rot, normals, t0, t1, t2))
                {
                    continue;
                }

                //voxel_half는 0.25f 이므로 소수점 2자리까지만 사용해도 된다.
                Vector3 A = targetTransform.TransformPoint(vertices[t0]);
                A = CMath.FloorToVector(A, 2);
                Vector3 B = targetTransform.TransformPoint(vertices[t1]);
                B = CMath.FloorToVector(B, 2);
                Vector3 C = targetTransform.TransformPoint(vertices[t2]);
                C = CMath.FloorToVector(C, 2);
                Vector3 AB = CMath.FloorToVector((A + B) * 0.5f, 2);
                Vector3 BC = CMath.FloorToVector((B + C) * 0.5f, 2);
                Vector3 CA = CMath.FloorToVector((C + A) * 0.5f, 2);

                //각 triangle당 6번씩 샘플링. other은 offset 만들기 위함
                SetTargetVoxelData(point: A,   to: AB,  other: C);
                SetTargetVoxelData(point: AB,  to: B,   other: C);
                SetTargetVoxelData(point: B,   to: BC,  other: A);
                SetTargetVoxelData(point: BC,  to: C,   other: A);
                SetTargetVoxelData(point: C,   to: CA,  other: B);
                SetTargetVoxelData(point: CA,  to: A,   other: B);

                //이쯤에서 확인해봅시다.
                Voxel_t2 voxel;
                if (PVoxel.Get(map, A,  out voxel)) { PrintDebug("A",  A,  voxel); }
                if (PVoxel.Get(map, AB, out voxel)) { PrintDebug("AB", AB, voxel); }
                if (PVoxel.Get(map, B,  out voxel)) { PrintDebug("B",  B,  voxel); }
                if (PVoxel.Get(map, BC, out voxel)) { PrintDebug("BC", BC, voxel); }
                if (PVoxel.Get(map, C,  out voxel)) { PrintDebug("C",  C,  voxel); }
                if (PVoxel.Get(map, CA, out voxel)) { PrintDebug("CA", CA, voxel); }
            }
        }
    }
    private void PrintDebug(string index, Vector3 point, Voxel_t2 voxel)
    {
        int h = (voxel.Data & 0b_11_11_11_11_11 << 8) >> 8;
        int m = voxel.Data & 0xFF;
        Debug.Log($"[{index}]{PVoxel.GetPivot(point):F3} h:{System.Convert.ToString(h, 2)}, m:{System.Convert.ToString(m, 2)}");
    }

    private bool IsValidTriangle(Quaternion rot, Vector3[] normals, int t0, int t1, int t2)
    {
        Vector3 normal1 = rot * normals[t0];
        Vector3 normal2 = rot * normals[t1];
        Vector3 normal3 = rot * normals[t2];

        Vector3 normal = normal1;
        if (normal2.y < normal.y) { normal = normal2; }
        if (normal3.y < normal.y) { normal = normal3; }
        normal = CMath.Floor1000Vector3(normal);

        return 0 < normal.y;
    }
    private void SetTargetVoxelData(Vector3 point, Vector3 to, Vector3 other)
    {
        Vector3 offset  = CMath.FloorToVector(((to - point) + (other - point)).normalized, 3) * 0.01f;
        Vector3 pivot   = PVoxel.GetPivot(point + offset);
        int key         = PVoxel.GetKeyFromPivot(pivot);

        //set data
        if (false == map.TryGetValue(key, out Voxel_t2 voxel))
        {
            map.Add(key, new Voxel_t2());
        }

        Vector3 diff = CMath.FloorToVector(point - pivot, 3);
        int x = VOXEL_HALF_SIZE < diff.x ? 0b_01 : 0;
        int z = VOXEL_HALF_SIZE < diff.z ? 0b_10 : 0;
        int quarant = x + z;

        int heightFlag = CMath.FloorToInt(diff.y * VOXEL_HALF_INVERT, 3);
        heightFlag <<= (2 * quarant) + 8;
        int movableFlag = 0b_11 << (2 * quarant);
        int newData = voxel.Data | heightFlag | movableFlag;
        map[key] = new Voxel_t2(newData);

        //set height-neighbor
        SetNeighborVoxelData();
    }
    private void SetNeighborVoxelData()
    { 
        
    }

    #region 높이 연산 예시 코드
    //3차원 공간에서 점 a, b, c를 포함하는 면의 방정식을 알 수 있을까? unity engine으로 게임을 개발하고 있어.
    //어떤 벡터의 x, z값을 이 방정식에 대입하여 y값을 구하고 싶어.가능하다면 코드도 함께 작성하여 알려줘.
    /*
         [SerializeField] private Vector2 point;
    [SerializeField] private GameObject obj;
    private void Start()
    {
        point = new Vector2(0.5f, 0.5f);
        obj.transform.position = new Vector3(point.x, 0f, point.y);
    }
    private void Update()
    {
        float x = point.x;
        float z = point.y;

        Vector3 pa = new Vector3(1, 0, 0);
        Vector3 pb = new Vector3(0, 0, 1);
        Vector3 pc = new Vector3(1, 1, 1);

        Vector3 ab = pb - pa;
        Vector3 ac = pc - pa;

        //얘를 캐싱해서 사용하면 되는거 같은데?
        Vector3 normal = Vector3.Cross(ab, ac).normalized;

        float A = normal.x;
        float B = normal.y;
        float C = normal.z;
        float D = Vector3.Dot(normal, pa);

        float y = (-A * x + -C * z + D) / B;
        Debug.Log(y);
        obj.transform.position = new Vector3(x, y, z);
    }
     */
    #endregion
}
