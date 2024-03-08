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
    public static Vector3 GetPivot(int key)
    {
        float x = (key & 0x_FF_0000) >> 16;
        float y = (key & 0x_00_FF00) >> 8;
        float z = (key & 0x_00_00FF);

        return new Vector3(x, y, z) * VOXEL_SIZE;
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

    public static int GetHeightFlag(Vector3 diff)
    {
        diff = CMath.FloorToVector(diff * VOXEL_HALF_INVERT, 2);
        int x = CMath.FloorToInt(diff.x, 1) << 2;
        int z = CMath.FloorToInt(diff.z, 1);
        int y = CMath.FloorToInt(diff.y, 1);

        switch (x | z)
        {
            case 0b_10_00: return y << (0 + VOXEL_BIT_HEIGHT);
            case 0b_10_10: return y << (2 + VOXEL_BIT_HEIGHT);
            case 0b_00_10: return y << (4 + VOXEL_BIT_HEIGHT);
            case 0b_00_00: return y << (6 + VOXEL_BIT_HEIGHT);
            case 0b_01_01: return y << (8 + VOXEL_BIT_HEIGHT);
        }

        return 0;
    }
    public static int GetMoveFlag(Vector3 diff)
    {
        int flag = 0;
        flag |= (diff.z > diff.x) ? 0b_10 : 0;
        flag |= (diff.z > -diff.x + VOXEL_SIZE) ? 0b_01 : 0;

        switch (flag)
        {
            case 0b_01: return 0b_0001;
            case 0b_11: return 0b_0010;
            case 0b_10: return 0b_0100;
            case 0b_00: return 0b_1000;
            default:    return 0;
        }
    }


    //need...?
    public static bool Get(Dictionary<int, Voxel_t2> map, Vector3 point, out Voxel_t2 voxel)
    {
        Vector3 pivot = GetPivot(point);
        int key = GetKeyFromPivot(pivot);

        if (map.TryGetValue(key, out voxel))
        {
            return true;
        }

        return false;
    }

    public static bool CompareHeight(int target, int neighbor, int shift)
    {
        target >>= 4;
        neighbor >>= 4;

        switch (shift)
        {
            case -(1 << 16): //(x-1)
                return ((target & 0b_11_00_00_00) >> 6) == (neighbor & 0b_00_00_00_11);
        }

        return false;
    }



    //no ref
    //public static bool Get(Dictionary<int, Voxel_t> map, Vector3 point, out Voxel_t voxel)
    //{
    //    Vector3 pivot = GetPivot(point);
    //    int key = GetKeyFromPivot(pivot);

    //    if (map.TryGetValue(key, out voxel))
    //    {
    //        return true;
    //    }

    //    return false;
    //}
    //public static int GetSubFromKey(Dictionary<int, Voxel_t> map, int key, Vector3 point)
    //{
    //    if (map.TryGetValue(key, out Voxel_t voxel))
    //    {
    //        Vector3 targetPivot = GetPivot(key);
    //        int idxSub = GetSubIndex(targetPivot, point);
    //        return voxel.GetSubType(idxSub);
    //    }

    //    return -1;
    //}
    //public static int GetSubIndex(Vector3 point)
    //{
    //    Vector3 pivot = GetPivot(point);
    //    return GetSubIndex(pivot, point);
    //}
}
