using UnityEngine;
using static Public;
using CMathf;
using CDataStructure;
using System.Collections.Generic;
using UnityEngine.Rendering;

/// <summary> Parser related to Voxel </summary>
public static class PVoxel
{
    public static Vector3 GetPivot(Vector3 point, int exponent = 2)
    {
        float cx = CMath.FloorToInt(point.x * VOXEL_INVERT, exponent) * VOXEL_SIZE;
        float cy = CMath.FloorToInt(point.y * VOXEL_INVERT, exponent) * VOXEL_SIZE;
        float cz = CMath.FloorToInt(point.z * VOXEL_INVERT, exponent) * VOXEL_SIZE;

        return new Vector3(cx, cy, cz);
    }
    public static Vector3 GetPivot(int key)
    {
        float x = (key >> 16)           * VOXEL_SIZE;
        float y = ((key >> 8) & 0x00FF) * VOXEL_SIZE;
        float z = (key & 0xFF)          * VOXEL_SIZE;

        return new Vector3(x, y, z);
    }
    public static int GetKey(Vector3 point)
    {
        Vector3 pivot = GetPivot(point);
        return (int)(pivot.x * VOXEL_INVERT) << 16 | (int)(pivot.y * VOXEL_INVERT) << 8 | (int)(pivot.z * VOXEL_INVERT);
    }
    public static int GetMoveFlag(Vector3 diff)
    {
        int index = 0;
        index |= (diff.z > diff.x) ? 0b_10 : 0;
        index |= (diff.z > -diff.x + VOXEL_SIZE) ? 0b_01 : 0;

        switch (index)
        {
            case 0b_01: index = 0; break;
            case 0b_11: index = 1; break;
            case 0b_10: index = 2; break;
            case 0b_00: index = 3; break;
            default: return -1;
        }

        return 1 << index;
    }

    public static int SetHeightFlag(Vector3 diff)
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
}
