using CDataStructure;
using CMathf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Public;

public class MapSampler6th : MonoBehaviour
{
    [Header("File")]
    [SerializeField] private string fileName;

    [Header("Voxels")]
    [SerializeField] private Transform resourceTransform;
    [SerializeField] private float samplingInterval;

    [Header("Gizmos")]
    [SerializeField] private Mesh gizmoMesh;
    [SerializeField] private float drawHeightLow;
    [SerializeField] private float drawHeightHigh;

    private Dictionary<int, Voxel_t2> map;
    private Dictionary<int, Voxel_t2> posted;
    private MeshFilter[] filter;

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
            DataTable.WriteBinaryMappingData(map, fileName);
            Debug.Log($"save: {fileName};");
        }
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (map != null)
            {
                map.Clear();
            }
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (map != null)
            {
                map = DataTable.LoadMappingData<Voxel_t2>(fileName);
                Debug.Log($"load: {fileName};");
            }
            else
            {
                Debug.Log("load: data is null;");
            }
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (map != null)
            {
                map.Clear();
            }
            filter = resourceTransform.GetComponentsInChildren<MeshFilter>();
            Sampling();
        }
    }

    private void Sampling()
    {
        IEnumerator sampling = Coroutiner.Play(SamplingVoxels());
        while (sampling.MoveNext())
        {
            Debug.Log($"Progress: {CMath.Floor1000((float)sampling.Current) * 100f:F1} %");
        }
    }
    private IEnumerator<float> SamplingVoxels()
    {
        map = new Dictionary<int, Voxel_t2>();

        for (int f = 0; f < filter.Length; ++f)
        {

            Transform targetTransform = filter[f].transform;
            if (targetTransform.CompareTag("MapObject"))
            {
                continue;
            }

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

                //The variable slopeFlag can have values ​​of -1, 0, 1, 2, 3.
                //There is special processing when slopeFlag == -1. (It means 'OBSTACLE')
                //The value 0xF0000 is a position that cannot be moved at all. So does not need to be saved.
                int slopeFlag = GetSlopeFlag(normal1, normal2, normal3);
                if (slopeFlag == 0xF0000)
                {
                    continue;
                }

                //Additional calculation of new Vector3(-0.001f, 0.001f, -0.001f) to reduce floating point issues
                Vector3 A = targetTransform.TransformPoint(vertices[triangles[t]])      + new Vector3(-0.001f, 0.001f, -0.001f);
                Vector3 B = targetTransform.TransformPoint(vertices[triangles[t + 1]])  + new Vector3(-0.001f, 0.001f, -0.001f);
                Vector3 C = targetTransform.TransformPoint(vertices[triangles[t + 2]])  + new Vector3(-0.001f, 0.001f, -0.001f);

                float distAB = Vector3.Distance(A, B);
                float interval = (VOXEL_SIZE > distAB) ? samplingInterval * 4f : samplingInterval;
                int samplingCountAB = CMath.FloorToInt1000(distAB / VOXEL_HALF_SIZE * interval);

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
                        Vector3 samplingPoint = CMath.Floor1000Vector3(Vector3.Lerp(AB, AC, ratio));
                        SetVoxel(slopeFlag, samplingPoint);
                    }
                }
            }

            yield return (float)(f + 1) / filter.Length;
        }

        Debug.Log($"Sampling count:({map.Keys.Count})");

        posted = new Dictionary<int, Voxel_t2>();
        foreach (int key in map.Keys)
        {
            Voxel_t2 voxel = map[key];
            if (voxel.TypeFlag != 0)
            {
                int linked = GetLinked(map, key);
                voxel.SetLinked(ref linked);

                posted.Add(key, voxel);

                Debug.Log($"{PVoxel.GetPivot(key)}.LINK == {System.Convert.ToString(posted[key].Link, 2)}");
            }
        }
    }

    private int GetSlopeFlag(Vector3 normal1, Vector3 normal2, Vector3 normal3)
    {
        //Find the normal value that serves as the standard.
        //To avoid floating point problems, round off at 3 decimal places.
        Vector3 normal = normal1;
        if (normal2.y < normal.y) { normal = normal2; }
        if (normal3.y < normal.y) { normal = normal3; }
        normal = CMath.Floor1000Vector3(normal);

        //Set Slope Degree Type (only 5 slope angles types. obstacle(can`t move) return -1;)
        int data;
        if      (normal.y ==  1.000f) { data = DEG_00 << VOXEL_BIT_DEG; }
        else if (normal.y ==  0.500f) { data = DEG_30 << VOXEL_BIT_DEG; }
        else if (normal.y ==  0.577f) { data = DEG_57 << VOXEL_BIT_DEG; }
        else if (normal.y ==  0.707f) { data = DEG_45 << VOXEL_BIT_DEG; }
        //else if (normal.y ==  0.898f) { data = DEG_64 << VOXEL_BIT_DEG; }
        else if (normal.y == -1.000f) { return -1; }
        else                          { return 0x000F_0000; }

        //else { return -1; }

        //Set Slope Diretion Flag
        //The direction opposite to the direction indicated by normal value is the direction in which the slope increases.
        if (data > 0)
        {
            if      (normal.x < 0) { data |= (0b_01_00) << VOXEL_BIT_DIR; }
            else if (normal.x > 0) { data |= (0b_11_00) << VOXEL_BIT_DIR; }
            if      (normal.z < 0) { data |= (0b_00_01) << VOXEL_BIT_DIR; }
            else if (normal.z > 0) { data |= (0b_00_11) << VOXEL_BIT_DIR; }
        }

        return data;
    }
    private void SetVoxel(int slopeFlag, Vector3 point)
    {
        int voxelKey    = PVoxel.GetKeyFromPoint(point);
        int shift = PVoxel.GetMoveIndex(point);
        int moveFlag = 1 << shift;
        int heightFlag = (slopeFlag & 0x0F00) != 0 ? 1 << (shift + VOXEL_BIT_HEIGHT) : 0;

        int newData = heightFlag | moveFlag;

        //add voxel
        if (false == map.TryGetValue(voxelKey, out Voxel_t2 voxel))
        {
            if (slopeFlag == -1)
            {
                newData = 0;
            }

            map.Add(voxelKey, new Voxel_t2(newData));
            return;
        }

        //update voxel
        newData = voxel.Data;
        int voxelType = voxel.TypeFlag >> VOXEL_BIT_TYPE;

        if (OBSTACLE != voxelType)
        {
            if (-1 == slopeFlag
                && PLAIN == voxelType)
            {
                newData &= ~(heightFlag | moveFlag);
                if ((newData & 0x000F) == 0)
                {
                    newData = 0;
                }
            }
            else
            {
                newData |= (slopeFlag | heightFlag | moveFlag);
            }

            map[voxelKey] = new Voxel_t2(newData);
        }
        else if (-1 != slopeFlag
                 && 0 != (slopeFlag & 0x0F00))
        {
            newData |= (slopeFlag | heightFlag | moveFlag);
            map[voxelKey] = new Voxel_t2(newData);
        }
    }
    private int GetLinked(Dictionary<int, Voxel_t2> map, int key)
    {
        int value = 0;

        for (int x = -1; x < 3; ++x)
        {
            for (int y = -1; y < 3; ++y)
            {
                for (int z = -1; z < 3; ++z)
                {
                    // 주변 복셀을 찾아가서 + 서브 복셀까지 체크
                    int addKey = (int)(x * VOXEL_INVERT) << 16 + (int)(y * VOXEL_INVERT) << 16 + (int)(z * VOXEL_INVERT);
                    if (map.TryGetValue(key + addKey, out Voxel_t2 targetVoxel))
                    {
                        int shift = 9 * x + 3 * y + z;

                        //y축 고려 안했니
                        int flag = 0b_00_00;
                        if (x == -1) { flag |= 0b_11_00; }
                        else if (x == 1) { flag |= 0b_01_00; }

                        if (z == -1) { flag |= 0b_00_11; }
                        else if (z == 1) { flag |= 0b_00_01; }

                        //자기자신을 0,0으로 잡았을 때의 x,z 축의 상대적 위치
                        switch (flag)
                        {
                            case 0b_00_00: //  0,  0
                                value |= 1 << shift;
                                break;

                            case 0b_00_01: //  0,  1
                                if (targetVoxel.CanMove(3))
                                    value |= 1 << shift;
                                break;
                            case 0b_01_00: //  1,  0
                                if (targetVoxel.CanMove(2))
                                    value |= 1 << shift;
                                break;
                            case 0b_00_11: //  0, -1
                                if (targetVoxel.CanMove(1))
                                    value |= 1 << shift;
                                break;
                            case 0b_11_00: // -1,  0
                                if (targetVoxel.CanMove(0))
                                    value |= 1 << shift;
                                break;

                            case 0b_11_01: // -1,  1
                                if (targetVoxel.CanMove(1) && targetVoxel.CanMove(4))
                                    value |= 1 << shift;
                                break;
                            case 0b_01_01: //  1,  1
                                if (targetVoxel.CanMove(2) && targetVoxel.CanMove(3))
                                    value |= 1 << shift;
                                break;
                            case 0b_11_11: // -1, -1
                                if (targetVoxel.CanMove(0) && targetVoxel.CanMove(1))
                                    value |= 1 << shift;
                                break;
                            case 0b_01_11: //  1, -1
                                if (targetVoxel.CanMove(1) && targetVoxel.CanMove(2))
                                    value |= 1 << shift;
                                break;
                        }
                    }
                }
            }
        }



        return value; 
    }

    private void OnDrawGizmos()
    {
        if (null == posted)
        {
            return;
        }
        foreach (int key in posted.Keys)
        {
            //get pivot > decide whether to draw or not.
            Vector3 pivot = PVoxel.GetPivot(key);
            if (pivot.y < drawHeightLow || pivot.y > drawHeightHigh)
            {
                Gizmos.color = Color.clear;
                continue;
            }

            //get voxel data
            Voxel_t2 voxel = posted[key];

            //draw outline
            Gizmos.color = Color.grey;
            Gizmos.DrawLine(pivot, pivot + Vector3.forward * VOXEL_SIZE);
            Gizmos.DrawLine(pivot, pivot + Vector3.right * VOXEL_SIZE);
            Gizmos.DrawLine(pivot + Vector3.forward * VOXEL_SIZE, pivot + new Vector3(1, 0, 1) * VOXEL_SIZE);
            Gizmos.DrawLine(pivot + Vector3.right * VOXEL_SIZE, pivot + new Vector3(1, 0, 1) * VOXEL_SIZE);

            Vector3 center = pivot + VOXEL_HALF_SIZE * new Vector3(1f, 0f, 1f);
            for (int i = 0; i < 4; ++i)
            {
                if (true == voxel.CanMove(moveFlag: 1 << i))
                {
                    Gizmos.color = Color.green;

                    if (true == voxel.HaveHeight(heightFlag: 1 << (i + VOXEL_BIT_HEIGHT)))
                    {
                        Gizmos.color = Color.blue;
                    }
                }
                else
                {
                    Gizmos.color = Color.red;
                }

                switch (i)
                {
                    case 0: Gizmos.DrawLine(center, center + Vector3.right * VOXEL_HALF_SIZE); break;
                    case 1: Gizmos.DrawLine(center, center + Vector3.forward * VOXEL_HALF_SIZE); break;
                    case 2: Gizmos.DrawLine(center, center + Vector3.left * VOXEL_HALF_SIZE); break;
                    case 3: Gizmos.DrawLine(center, center + Vector3.back * VOXEL_HALF_SIZE); break;
                }
            }

        }
    }
}
