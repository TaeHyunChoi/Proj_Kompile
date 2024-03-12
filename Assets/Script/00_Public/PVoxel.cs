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
        int quarant = GetMoveQuarant(diff);
        return 1 << quarant;
    }
    public static int GetMoveQuarant(Vector3 diff)
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

        return index;
    }
    public static float GetYValue(Voxel_t voxel, Vector3 point)
    {
        Vector3 pivot = GetPivot(point);

        int quarant = GetMoveQuarant(point - pivot);

        //set y value
        Vector3 p0 = voxel.GetYValue((quarant + 4) % 4) * Vector3.up;
        Vector3 p1 = voxel.GetYValue((quarant + 5) % 4) * Vector3.up;
        Vector3 pm = pivot + new Vector3(1, 0, 1) * VOXEL_HALF_SIZE + voxel.GetYValue(4) * Vector3.up;

        //set x,z value
        switch (quarant)
        {
            case 0:
                p0 += pivot + new Vector3(1, 0, 0) * VOXEL_SIZE;
                p1 += pivot + new Vector3(1, 0, 1) * VOXEL_SIZE;
                break;
            case 1:
                p0 += pivot + new Vector3(1, 0, 1) * VOXEL_SIZE;
                p1 += pivot + new Vector3(0, 0, 1) * VOXEL_SIZE;
                break;
            case 2:
                p0 += pivot + new Vector3(0, 0, 1) * VOXEL_SIZE;
                p1 += pivot;
                break;
            case 3:
                p0 += pivot;
                p1 += pivot + new Vector3(0, 0, 1) * VOXEL_SIZE;
                break;
        }

        //get normal
        p0 = CMath.FloorToVector(p0, 3);
        p1 = CMath.FloorToVector(p1, 3);
        pm = CMath.FloorToVector(pm, 3);
        Debug.Log($"coord p0:{p0:F3}, p1:{p1:F3}, pm:{pm:F3}");

        Vector3 normal = Vector3.Cross(p1 - pm, p0 - pm);
        normal.Normalize();
        normal = CMath.FloorToVector(normal, 3);

        if (0f == normal.y)
        {
            Debug.Log("normal.y == 0f;");
            return 0f;
        }

        //vector equation of the plane
        float y = -(normal.x * point.x + normal.z * point.z - Vector3.Dot(normal, pm)) / normal.y;
        y = CMath.Floor(y, 3);

        Debug.Log($"{p0.y}, {p1.y}, {pm.y} => normal:{normal.y:F3} => y:{y:F3}");
        //Debug.Log($"(normal:{normal}) => {y:F3} = ({-normal.x}*{point.x} + {-normal.z}*{point.z} + {Vector3.Dot(normal, point)})/{normal.y}");

        return y;
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
