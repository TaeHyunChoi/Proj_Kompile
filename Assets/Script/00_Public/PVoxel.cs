using UnityEngine;
using static Public;
using CMathf;
using CDataStructure;
using System.Collections.Generic;
using UnityEngine.Rendering;

/// <summary> Parser related to Voxel </summary>
public static class PVoxel
{
    public static Vector3 GetPivot(Vector3 point)
    {
        float cx = CMath.Floor1000(CMath.FloorToInt1000(point.x * VOXEL_INVERT) * VOXEL_SIZE);
        float cy = CMath.Floor1000(CMath.FloorToInt1000(point.y * VOXEL_INVERT) * VOXEL_SIZE);
        float cz = CMath.Floor1000(CMath.FloorToInt1000(point.z * VOXEL_INVERT) * VOXEL_SIZE);

        return new Vector3(cx, cy, cz);
    }
    public static bool Get(Dictionary<int, Voxel_t> map, Vector3 point, out Voxel_t voxel)
    {
        Vector3 pivot = GetPivot(point);
        int key = GetKeyFromPivot(pivot);

        if (map.TryGetValue(key, out voxel))
        {
            return true;
        }

        return false;
    }
    public static int GetKeyFromPoint(Vector3 point)
    {
        Vector3 pivot = GetPivot(point);
        return GetKeyFromPivot(pivot);
    }
    public static int GetKeyFromPivot(Vector3 pivot)
    {
        return (int)(pivot.x * VOXEL_INVERT) << 16 | (int)(pivot.y * VOXEL_INVERT) << 8 | (int)(pivot.z * VOXEL_INVERT);
    }
    public static int GetMoveIndex(Vector3 point)
    {
        int index = -1;
        Vector3 pivot = GetPivot(point);

        bool e1 = (point.z - pivot.z) >  (point.x - pivot.x);
        bool e2 = (point.z - pivot.z) > -(point.x - pivot.x) + VOXEL_SIZE;
        if      (!e1 & e2)  { index = 0; }
        else if (e1 & e2)   { index = 1; }
        else if (e1 & !e2)  { index = 2; }
        else if (!e1 & !e2) { index = 3; }

        return index;
    }
    public static int GetSubIndex(Vector3 pivot, Vector3 point)
    {
        int index = -1;

        bool e1 = (point.z - pivot.z) > (point.x - pivot.x);
        bool e2 = (point.z - pivot.z) > -(point.x - pivot.x) + VOXEL_SIZE;
        if      (!e1 &  e2) { index = 0; }
        else if ( e1 &  e2) { index = 1; }
        else if ( e1 & !e2) { index = 2; }
        else if (!e1 & !e2) { index = 3; }

        return index;
    }

    public static Vector3 GetPivot(int key)
    {
        float x = (key & 0x_FF_0000) >> 16;
        float y = (key & 0x_00_FF00) >> 8;
        float z = (key & 0x_00_00FF);

        return new Vector3(x, y, z) * VOXEL_SIZE;
    }
    public static int GetSubFromKey(Dictionary<int, Voxel_t> map, int key, Vector3 point)
    {
        if (map.TryGetValue(key, out Voxel_t voxel))
        {
            Vector3 targetPivot = GetPivot(key);
            int idxSub = GetSubIndex(targetPivot, point);
            return voxel.GetSubType(idxSub);
        }

        return -1;
    }
    public static int GetSubIndex(Vector3 point)
    {
        Vector3 pivot = GetPivot(point);
        return GetSubIndex(pivot, point);
    }
}
