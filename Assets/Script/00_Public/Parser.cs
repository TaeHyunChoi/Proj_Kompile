using UnityEngine;
using static Public;
using CMathf;
using CDataStructure;
using System.Collections.Generic;

public static class Parser
{
    public static Vector3 GetVoxelPivot(Vector3 point)
    {
        float cx = CMath.Floor1000(CMath.FloorToInt1000(point.x * VOXEL_INVERT) * VOXEL_SIZE);
        float cy = CMath.Floor1000(CMath.FloorToInt1000(point.y * VOXEL_INVERT) * VOXEL_SIZE);
        float cz = CMath.Floor1000(CMath.FloorToInt1000(point.z * VOXEL_INVERT) * VOXEL_SIZE);

        return new Vector3(cx, cy, cz);
    }
    public static bool GetVoxel(Dictionary<int, Voxel_t> map, Vector3 point, out Voxel_t voxel)
    {
        Vector3 pivot = GetVoxelPivot(point);
        int key = GetVoxelKeyFromPivot(pivot);

        if (map.TryGetValue(key, out voxel))
        {
            return true;
        }

        return false;
    }
    public static int GetVoxelKeyFromPoint(Vector3 point)
    {
        Vector3 pivot = GetVoxelPivot(point);
        return GetVoxelKeyFromPivot(pivot);
    }
    public static int GetVoxelKeyFromPivot(Vector3 pivot)
    {
        return (int)(pivot.x * VOXEL_INVERT) << 16 | (int)(pivot.y * VOXEL_INVERT) << 8 | (int)(pivot.z * VOXEL_INVERT);
    }
    public static int GetSubVoxelIndex(Vector3 pivot, Vector3 point)
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
}
