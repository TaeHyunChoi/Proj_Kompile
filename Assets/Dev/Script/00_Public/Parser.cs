using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Public;

public static class Parser
{
    public static Vector3 GetCenterPoint(Vector3 point)
    {
        float cx = Mathf.Floor(point.x * HALF_GRID_SIZE_INVERT) * HALF_GRID_SIZE;
        float cy = Mathf.Floor(point.y * GRID_SIZE_INVERT) * 2f * HALF_GRID_SIZE + 1 * HALF_GRID_SIZE;
        float cz = Mathf.Floor(point.z * HALF_GRID_SIZE_INVERT) * HALF_GRID_SIZE;

        Vector3 center;
        Vector3 p1, p2;

        if ((cz - cx) % GRID_SIZE == 0)
        {
            p1 = new Vector3(cx, cy, cz);                                    // half-size clamp
            p2 = new Vector3(cx + HALF_GRID_SIZE, cy, cz + HALF_GRID_SIZE);  // half-size clamp + new Vector3(1,0,1);
        }
        else
        {
            p1 = new Vector3(cx + HALF_GRID_SIZE, cy, cz);     // half-size clamp + Vector3.right
            p2 = new Vector3(cx, cy, cz + HALF_GRID_SIZE);     // half-size clamp + Vector3.up
        }

        if (Vector3.Distance(point, p1) <= Vector3.Distance(point, p2))
        {
            center = p1;
        }
        else
        {
            center = p2;
        }

        return center;
    }
    public static int GetVoxelRadix(Vector3 center)
    {
        int radix = (int)(center.x * HALF_GRID_SIZE_INVERT) << 16
            | (int)(center.y * HALF_GRID_SIZE_INVERT) << 8
            | (int)(center.z * HALF_GRID_SIZE_INVERT) << 0;

        return radix;
    }
    public static VoxelType GetVoxelType(Voxel_t data, Vector3 diff)
    {
        float angle = Mathf.Atan2(diff.z, diff.x) * Mathf.Rad2Deg;
        angle = (angle + 360) % 360;

        int shift;
        if (angle >= 0 && angle < 90) { shift = 0; }
        else if (angle >= 90 && angle < 180) { shift = 1; }
        else if (angle >= 180 && angle < 270) { shift = 2; }
        else { shift = 3; }

        //여기서 !Movable이 나오면 호출 함수로 돌아가 이웃 복셀을 검토한다.

        //if (diff.y <= 0)
        //{
        //    if (angle >= 0 && angle < 90) { shift = 0; }
        //    else if (angle >= 90 && angle < 180) { shift = 1; }
        //    else if (angle >= 180 && angle < 270) { shift = 2; }
        //    else { shift = 3; }
        //}
        //else
        //{
        //    if (angle >= 0 && angle < 90) { shift = 4; }
        //    else if (angle >= 90 && angle < 180) { shift = 5; }
        //    else if (angle >= 180 && angle < 270) { shift = 6; }
        //    else { shift = 7; }
        //}

        return data.GetSubType(shift);
    }
}
