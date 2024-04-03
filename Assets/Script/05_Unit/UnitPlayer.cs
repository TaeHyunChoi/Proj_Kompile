using System.Collections.Generic;
using UnityEngine;
using static Public;
using DataType;
using CMathf;

/// <summary>
/// It would be better to attach it as a "movement operation" component class in the future. The size is quite large;
/// </summary>
public class UnitPlayer : Unit
{
//    private readonly int[] intervalRot = new int[] { 0, 1, -1, 2, -2 }; //시계 방향을 우선 탐색하는 기준
//    private Vector3 beforeDir;

//    public void Move(Dictionary<int, Tile_t> map, Vector3 inputDir)
//    {
//        inputDir.Normalize();

//        Vector3 nowPoint = transform.position;
//        Vector3 targetPoint;
//        Vector3 dir;

//        float dist = CMath.Floor(Time.deltaTime * SPEED_MOVE, 3);
//        int sign;


//        //CHECK_INPUT_DIR:
//        for (int c = 0; c < 3; ++c)
//        {
//            dir = Quaternion.Euler(0f, intervalRot[c] * 45f, 0f) * inputDir;
//            dir.Normalize();
//            dir = CMath.FloorToVector(dir * (TILE_QUATER + dist), 3);

//            targetPoint = CMath.FloorToVector(nowPoint + dir, 3);

//            if (targetPoint.x < 0 || targetPoint.z < 0
//                || false == CanMove(map, nowPoint, targetPoint, out targetPoint))
//            {
//                goto CHECK_OTHER_DIRS;
//            }
//        }
//        dir = CMath.FloorToVector(inputDir, 3);
//        goto SET_POSITION;


//    CHECK_OTHER_DIRS:
//        float rotY = Mathf.Sign(Vector3.Cross(inputDir, beforeDir).y);
//        if (rotY >= 0) { sign =  1; }
//        else           { sign = -1; }

//        for (int i = 0; i < 5; ++i)
//        {
//            Vector3 inputRotDir = Quaternion.Euler(0f, intervalRot[i] * 45f, 0f) * inputDir;
//            inputRotDir.Normalize();
//            inputRotDir = CMath.FloorToVector(dir * (TILE_QUATER + dist), 3);

//            for (int d = 1; d < 5; ++d)
//            {
//                Vector3 otherDir = Quaternion.Euler(0f, sign * intervalRot[d] * 45f, 0f) * inputRotDir;

//                for (int c = 0; c < 3; ++c)
//                {
//                    dir = Quaternion.Euler(0f, intervalRot[c] * 45f, 0f) * otherDir;
//                    dir.Normalize();
//                    dir = CMath.FloorToVector(dir * (TILE_QUATER + dist), 3);

//                    targetPoint = CMath.FloorToVector(nowPoint + dir, 3);

//                    if (targetPoint.x < 0 || targetPoint.z < 0
//                        || false == CanMove(map, nowPoint, targetPoint, out targetPoint))
//                    {
//                        goto CONTINUE;
//                    }
//                }

//                otherDir.Normalize();
//                dir = CMath.FloorToVector(otherDir, 3);
//                goto SET_POSITION;

//            CONTINUE:
//                continue;
//            }
//        }

//#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
//        Debug.LogError("Can`t MOVE");
//#endif

//    SET_POSITION:
//        dir = CMath.FloorToVector(dir * dist, 3);
//        targetPoint = CMath.FloorToVector(nowPoint + dir, 3);

//        if (false == CanMove(map, nowPoint, targetPoint, out targetPoint))
//        {
//            return;
//        }

//        transform.position = targetPoint;
//        beforeDir = dir;
//    }
//    private bool IsMovableVoxel(Dictionary<int, Tile_t> map, int fromKey, int targetKey, Vector3 toPoint, out Tile_t targetVoxel)
//    {
//        if (false == map.TryGetValue(targetKey, out targetVoxel))
//            return false;

//        if (false == targetVoxel.IsLinkedWith(fromKey, targetKey))
//            return false;

//        if (false == targetVoxel.IsMovable(toPoint))
//            return false;

//        return true;
//    }
//    private bool CanMove(Dictionary<int, Tile_t> map, Vector3 from, Vector3 to, out Vector3 point)
//    {
//        point = Vector3.zero;
//        int keyFrom = PVoxel.GetKey(from);
//        int keyTo   = PVoxel.GetKey(to);

//        //y ==
//        if (true == IsMovableVoxel(map, keyFrom, keyTo, to, out Tile_t voxelTo))
//        {
//            float y = PVoxel.GetYValue(voxelTo, to);
//            point = CMath.FloorToVector(new Vector3(to.x, y, to.z), 3);
//            return true;
//        }

//        //y ++
//        Vector3 newTo = to + Vector3.up * TILE_HALF;
//        keyTo = PVoxel.GetKey(newTo);
//        if (true == IsMovableVoxel(map, keyFrom, keyTo, newTo, out voxelTo))
//        {
//            float y = PVoxel.GetYValue(voxelTo, newTo);
//            point = CMath.FloorToVector(new Vector3(newTo.x, y, newTo.z), 3);
//            return true;
//        }

//        //y --
//        newTo = to - Vector3.up * TILE_HALF;
//        keyTo = PVoxel.GetKey(newTo);
//        if (true == IsMovableVoxel(map, keyFrom, keyTo, newTo, out voxelTo))
//        {
//            float y = PVoxel.GetYValue(voxelTo, newTo);
//            point = CMath.FloorToVector(new Vector3(newTo.x, y, newTo.z), 3);
//            return true;
//        }

//        return false;
//    }
}