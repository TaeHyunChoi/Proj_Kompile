using System.Collections.Generic;
using UnityEngine;
using static Public;
using CDataStructure;
using CMathf;
using Unity.VisualScripting;

/// <summary>
/// It would be better to attach it as a "movement operation" component class in the future. The size is quite large;
/// </summary>
public class UnitPlayer : Unit
{
    private readonly int[] intervalRot = new int[] { 0, 1, -1, 2, -2 }; //시계 방향을 우선 탐색하는 기준
    private Vector2 before;

    public void Move(Dictionary<int, Voxel_t> map, Vector3 inputDir)
    {
        inputDir.Normalize();
        Vector3 nowPoint = transform.position;

        int sign = 1;
        if (before.x < 0)   { sign *= -1; }
        if (inputDir.z < 0) { sign *= -1; }

        //Debug.Log($"[{sign}{inputDir:F0}] {intervalRot[0]}, {intervalRot[1]}, {intervalRot[2]}, {intervalRot[3]}, {intervalRot[4]}");

        float dist = CMath.Floor(Time.deltaTime * MOVE_SPEED, 3);
        for (int i = 0; i < intervalRot.Length; ++i) 
        {
            Vector3 rotDir = Quaternion.Euler(0f, sign * intervalRot[i] * 45f, 0f) * inputDir;
            rotDir.Normalize();

            //## Check collision only up to 45 degrees
            Vector3 colDir, colPoint;
            for (int j = 0; j < 3; ++j) 
            {
                colDir = Quaternion.Euler(0f, sign * intervalRot[j] * 45f, 0f) * rotDir;
                colDir.Normalize();
                colDir = CMath.FloorToVector(colDir * (VOXEL_QUATER_SIZE - dist), 3);
                colPoint = CMath.FloorToVector(nowPoint + colDir, 3);

                if (colPoint.x < 0 || colPoint.z < 0
                    || false == MoveTo(map, nowPoint, colPoint, out colPoint))
                {
                    Debug.Log($"Collided[{j}] {colPoint:F3}");
                    goto CONTINUE;
                }
            }

            //## check Move
            rotDir = CMath.FloorToVector(rotDir * dist, 3);
            Vector3 targetPoint = CMath.FloorToVector(nowPoint + rotDir, 3);

            if (true == MoveTo(map, nowPoint, targetPoint, out Vector3 point))
            {
                //min, max
                if (point.x < 0)
                    point = new Vector3(0, point.y, point.z);
                if (point.z < 0)
                    point = new Vector3(point.x, point.y, 0);

                transform.position = point;
                before = new Vector2(inputDir.x, inputDir.z);
                return;
            }

        CONTINUE:
            continue;
        }
    }
    private bool IsMovable(Dictionary<int, Voxel_t> map, int fromKey, int targetKey, Vector3 toPoint, out Voxel_t targetVoxel)
    {
        if (false == map.TryGetValue(targetKey, out targetVoxel))
            return false;

        if (false == targetVoxel.IsLinkedWith(fromKey, targetKey))
            return false;

        if (false == targetVoxel.CanMoveTo(toPoint))
            return false;

        return true;
    }
    private bool MoveTo(Dictionary<int, Voxel_t> map, Vector3 from, Vector3 to, out Vector3 point)
    {
        point = Vector3.zero;
        int keyFrom = PVoxel.GetKey(from);
        int keyTo   = PVoxel.GetKey(to);

        //y ==
        if (true == IsMovable(map, keyFrom, keyTo, to, out Voxel_t voxelTo))
        {
            float y = PVoxel.GetYValue(voxelTo, to);
            point = CMath.FloorToVector(new Vector3(to.x, y, to.z), 3);
            return true;
        }

        Vector3 newTo;

        //y ++
        newTo = to + Vector3.up * VOXEL_HALF_SIZE;
        keyTo = PVoxel.GetKey(newTo);
        if (true == IsMovable(map, keyFrom, keyTo, newTo, out voxelTo))
        {
            float y = PVoxel.GetYValue(voxelTo, newTo);
            point = CMath.FloorToVector(new Vector3(newTo.x, y, newTo.z), 3);
            return true;
        }

        //y --
        newTo = to - Vector3.up * VOXEL_HALF_SIZE;
        keyTo = PVoxel.GetKey(newTo);
        if (true == IsMovable(map, keyFrom, keyTo, newTo, out voxelTo))
        {
            float y = PVoxel.GetYValue(voxelTo, newTo);
            point = CMath.FloorToVector(new Vector3(newTo.x, y, newTo.z), 3);
            return true;
        }

        return false;
    }
}