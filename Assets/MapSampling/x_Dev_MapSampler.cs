using System.Collections.Generic;
using UnityEngine;
using DataStruct;
using CMathf;
using System;

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
[Obsolete]
public class x_Dev_MapSampler : MonoBehaviour
{
    /*
    [SerializeField] private Transform transformRsc;
    [SerializeField] private GeometryUtility tester;
    private static Dictionary<int, STile> map = new Dictionary<int, STile>();
    public static Dictionary<int, STile> Map { get => map; }
    
    private void Start()
    {
        //// initialize tiles
        List<int> keys = new List<int>();
        foreach (int key in map.Keys)
        {
            keys.Add(key);
            DebugLog(key);
        }

        //// save data
        DataTable.WriteBinaryMappingData<STile>(map, transformRsc.GetChild(0).gameObject.name);
        Debug.Log($"Sampling Done.");
    }
    public static void InitTile(Transform transform, Mesh mesh, float scale, byte layer, int info, int trigger)
    {
        Quaternion rot = transform.rotation;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;

        for (int t = 0; t < triangles.Length; t += 3)
        {
            int t0 = triangles[t];
            int t1 = triangles[t + 1];
            int t2 = triangles[t + 2];

            //Determine whether the mesh is the target for sampling by normal value.
            Vector3 normal1 = rot * normals[t0];
            Vector3 normal2 = rot * normals[t1];
            Vector3 normal3 = rot * normals[t2];
            Vector3 normal = normal1;
            if (normal2.y < normal.y) { normal = normal2; }
            if (normal3.y < normal.y) { normal = normal3; }
            normal = CMath.FloorToVector(normal, 3);
            if (0 >= normal.y)
            {
                continue;
            }

            Vector3 A = TileUtility.SnappingPoint(transform.TransformPoint(vertices[t0]), 0.125f, 3);
            Vector3 B = TileUtility.SnappingPoint(transform.TransformPoint(vertices[t1]), 0.125f, 3);
            Vector3 C = TileUtility.SnappingPoint(transform.TransformPoint(vertices[t2]), 0.125f, 3);

            SetTileDataRecursive(A, B, C, scale, layer, info, trigger);
        }
    }

    // get
    private static long GetHeightFlag(Vector3 diff, float size_quater_inverse)
    {
        //diff = PTile.SnappingPoint(diff, size_quater, 3);
        diff = CMath.FloorToVector(diff, 3);
        int x  = (int) (diff.x * size_quater_inverse);
        long y = (long)(diff.y * size_quater_inverse);  //y: 0 ~ 4 (0b000 ~ 0b100)
        int z  = (int) (diff.z * size_quater_inverse);

        int shift;
        switch (x * 10 + z)
        {
            case 00: shift = 0; break;
            case 20: shift = 1; break;
            case 40: shift = 2; break;
            case 02: shift = 3; break;
            case 22: shift = 4; break;
            case 42: shift = 5; break;
            case 04: shift = 6; break;
            case 24: shift = 7; break;
            case 44: shift = 8; break;
            case 11: shift = 9; break;
            case 31: shift = 10; break;
            case 13: shift = 11; break;
            case 33: shift = 12; break;
            default:
                Debug.LogError($"{diff:F3} {x},{z} => {y}");
                return 0;
        }
        shift *= 3;

        return y << shift;
    }
    private static void SetTileDataRecursive(Vector3 p0, Vector3 p1, Vector3 p2, float scale, byte layer, int info, int trigger)
    {
        float v0to1 = CMath.Floor(Vector3.Distance(new Vector3(p0.x, 0, p0.z), new Vector3(p1.x, 0, p1.z)), 3);
        float v1to2 = CMath.Floor(Vector3.Distance(new Vector3(p1.x, 0, p1.z), new Vector3(p2.x, 0, p2.z)), 3);
        float v0to2 = CMath.Floor(Vector3.Distance(new Vector3(p0.x, 0, p0.z), new Vector3(p2.x, 0, p2.z)), 3);

        float diagonal = v1to2;
        Vector3 swap;

        //빠른 탐색을 위하여 꼭지점의 각이 직각인 점을 v0로 설정한다. (모든 삼각형이 직각 이등변 삼각형이라 가능함.)
        if (diagonal < v0to1)
        {
            swap = p2;
            p2 = p0;
            p0 = swap;

            diagonal = v0to1;
        }
        if (diagonal < v0to2)
        {
            swap = p1;
            p1 = p0;
            p0 = swap;

            diagonal = v0to2;
        }

        float scale_half   = TileUtility.GetScale(ETileSizeType.Half, scale);
        float scale_quater = scale_half * 0.5f;

        //삼각형 중 가장 긴 변이 단위 길이(scale_half)보다 같거나 짧을 때까지 재귀호출
        if (scale_half < diagonal)
        {
            Vector3 midPoint = CMath.FloorToVector((p1 + p2) * 0.5f, 3);
            SetTileDataRecursive(p0, p1, midPoint, scale, layer, info, trigger);
            SetTileDataRecursive(p0, p2, midPoint, scale, layer, info, trigger);
        }
        //대상 삼각형을 찾으면 Tile_t에 정보를 저장한다.
        else
        {
            //get point, get pivot
            Vector3 pointCenter = TileUtility.SnappingPoint((p0 + p1 + p2) * 0.333f, scale_quater * 0.5f, 3);
            Vector3 pivot = TileUtility.GetPivotByPoint(pointCenter, scale);

            //set flag
            int move = 1 << TileUtility.GetTriangleIndex(pointCenter - pivot, scale_half);

            long height = 0;
            float size_quater_inverse = TileUtility.GetScale(ETileSizeType.Quater_inverse, scale);
            height |= GetHeightFlag(p0 - pivot, size_quater_inverse);
            height |= GetHeightFlag(p1 - pivot, size_quater_inverse);
            height |= GetHeightFlag(p2 - pivot, size_quater_inverse);

            //set tile data
            int key = TileUtility.GetKeyByPoint(layer, pivot, scale);
            if (false == map.TryGetValue(key, out STile tile))
            {
                map.Add(key, new STile(info, trigger, move, height));
            }
            else
            {
                info |= tile.Info;
                trigger   |= tile.Trigger;
                move   |= tile.Move;
                height |= tile.Height;
                map[key] = new STile(info, trigger, move, height);
            }
        }
    }

    // utility
    private void DebugLog(int key)
    {
        STile tile = map[key];

        string info = tile.Info.ToString();
        string trigger = string.Empty;

        if (true == tile.HasTrigger(ETileTriggerType.Scale, out int not_used))
        {
            trigger += "Scale Down, ";
        }
        if (true == tile.HasTrigger(ETileTriggerType.Layer, out not_used))
        {
            trigger += "Layer, ";
        }
        if (true == tile.HasTrigger(ETileTriggerType.Event, out not_used))
        {
            trigger += "Interact, ";
        }

        string move   = System.Convert.ToString(tile.Move, 2).ToString();
        string height = System.Convert.ToString(tile.Height, 2).ToString();
        string link   = System.Convert.ToString(tile.Trigger & 0xFFF, 2);
        float scale = tile.GetScale(ETileSizeType.Default);
        Debug.Log($"{key}:{TileUtility.GetPivotByKey(key, scale):F3}(scale:{scale}, info:{info} trigger:{trigger}) m:{move} l:{link}\nh:{height}");
    }
    */
}
#endif