using System.Collections.Generic;
using UnityEngine;
using static Public;
using CDataStructure;
using CMathf;

/// <summary>
/// It would be better to attach it as a "movement operation" component class in the future. The size is quite large;
/// </summary>
public class UnitPlayer : Unit
{
    private readonly int[] intervalRot = new int[] { 0, 1, -1, 2, -2 }; //시계 방향을 우선 탐색하는 기준
    private int flagBefore;

    public void Move(Dictionary<int, Voxel_t> map, Vector3 inputDir)
    {
        inputDir.Normalize();

        Vector3 nowPoint = transform.position;
        Vector3 targetPoint;
        Vector3 dir;

        float dist = CMath.Floor(Time.deltaTime * MOVE_SPEED, 3);
        int sign;

        
    //CHECK_INPUT_DIR:
        for (int c = 0; c < 3; ++c)
        {
            dir = Quaternion.Euler(0f, intervalRot[c] * 45f, 0f) * inputDir;
            dir.Normalize();
            dir = CMath.FloorToVector(dir * (VOXEL_QUATER_SIZE + dist), 3);

            targetPoint = CMath.FloorToVector(nowPoint + dir, 3);

            if (targetPoint.x < 0 || targetPoint.z < 0
                || false == CanMove(map, nowPoint, targetPoint, out targetPoint))
            {
                goto CHECK_OTHER_DIRS;
            }
        }
        dir = CMath.FloorToVector(inputDir, 3);
        goto SET_POSITION;


    CHECK_OTHER_DIRS:
        sign = 1;
        if ((flagBefore >> 2)    > 0b_01) { sign *= -1; } // before.x < 0
        if ((flagBefore & 0b_11) > 0b_01) { sign *= -1; } // before.z < 0

        for (int d = 1; d < 5; ++d)
        {
            Vector3 otherDir = Quaternion.Euler(0f, sign * intervalRot[d] * 45f, 0f) * inputDir;

            for (int c = 0; c < 3; ++c)
            {
                dir = Quaternion.Euler(0f, intervalRot[c] * 45f, 0f) * otherDir;
                dir.Normalize();
                dir = CMath.FloorToVector(dir * (VOXEL_QUATER_SIZE + dist), 3);

                targetPoint = CMath.FloorToVector(nowPoint + dir, 3);

                if (targetPoint.x < 0 || targetPoint.z < 0
                    || false == CanMove(map, nowPoint, targetPoint, out targetPoint))
                {
                    goto CONTINUE;
                }
            }

            otherDir.Normalize();
            dir = CMath.FloorToVector(otherDir, 3);
            goto SET_POSITION;

        CONTINUE:
            continue;
        }


    SET_POSITION:
        dir = CMath.FloorToVector(dir * dist, 3);
        targetPoint = CMath.FloorToVector(nowPoint + dir, 3);

        if (false == CanMove(map, nowPoint, targetPoint, out targetPoint))
        {
            //Debug.Log($"{nowPoint:F3}.CANNOT_MOVE {targetPoint:F3}");
            return;
        }

        //Debug.Log($"{nowPoint:F3}.SET_POSITION {targetPoint:F3}");
        transform.position = targetPoint;

    //SET_LAST_DIR:
        int flag;
        if      (dir.x > 0) { flag  = 0b_01_00; }
        else if (dir.x < 0) { flag  = 0b_11_00; }
        else                { flag  = flagBefore & 0b_11_00; }

        if      (dir.z > 0) { flag |= 0b_00_01; }
        else if (dir.z < 0) { flag |= 0b_00_11; }
        else                { flag |= flagBefore & 0b_00_11; }

        flagBefore = flag;
    }
    private bool IsMovableVoxel(Dictionary<int, Voxel_t> map, int fromKey, int targetKey, Vector3 toPoint, out Voxel_t targetVoxel)
    {
        if (false == map.TryGetValue(targetKey, out targetVoxel))
            return false;

        if (false == targetVoxel.IsLinkedWith(fromKey, targetKey))
            return false;

        if (false == targetVoxel.CanMoveTo(toPoint))
            return false;

        return true;
    }
    private bool CanMove(Dictionary<int, Voxel_t> map, Vector3 from, Vector3 to, out Vector3 point)
    {
        point = Vector3.zero;
        int keyFrom = PVoxel.GetKey(from);
        int keyTo   = PVoxel.GetKey(to);

        //y ==
        if (true == IsMovableVoxel(map, keyFrom, keyTo, to, out Voxel_t voxelTo))
        {
            float y = PVoxel.GetYValue(voxelTo, to);
            point = CMath.FloorToVector(new Vector3(to.x, y, to.z), 3);
            return true;
        }

        Vector3 newTo;

        //y ++
        newTo = to + Vector3.up * VOXEL_HALF_SIZE;
        keyTo = PVoxel.GetKey(newTo);
        if (true == IsMovableVoxel(map, keyFrom, keyTo, newTo, out voxelTo))
        {
            float y = PVoxel.GetYValue(voxelTo, newTo);
            point = CMath.FloorToVector(new Vector3(newTo.x, y, newTo.z), 3);
            return true;
        }

        //y --
        newTo = to - Vector3.up * VOXEL_HALF_SIZE;
        keyTo = PVoxel.GetKey(newTo);
        if (true == IsMovableVoxel(map, keyFrom, keyTo, newTo, out voxelTo))
        {
            float y = PVoxel.GetYValue(voxelTo, newTo);
            point = CMath.FloorToVector(new Vector3(newTo.x, y, newTo.z), 3);
            return true;
        }

        return false;
    }
}