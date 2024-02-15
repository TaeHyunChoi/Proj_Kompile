using System.Collections.Generic;
using UnityEngine;
using static Public;
using PublicValue;

public class UnitPlayer : Unit
{
    //Cached data
    private readonly Quaternion[] inputRotation = new Quaternion[9] { Quaternion.AngleAxis(  -90f, Vector3.up) ,
                                                                      Quaternion.AngleAxis(-67.5f, Vector3.up),
                                                                      Quaternion.AngleAxis(  -45f, Vector3.up),
                                                                      Quaternion.AngleAxis(-22.5f, Vector3.up),
                                                                      Quaternion.AngleAxis(    0f, Vector3.up),
                                                                      Quaternion.AngleAxis( 22.5f, Vector3.up),
                                                                      Quaternion.AngleAxis(   45f, Vector3.up),
                                                                      Quaternion.AngleAxis( 67.5f, Vector3.up),
                                                                      Quaternion.AngleAxis(   90f, Vector3.up)};

    public void Move(Dictionary<int, Voxel_t5> map, Vector3 inputDir)
    {
        inputDir.Normalize();
        Vector3 position = transform.position;
        float delta = Time.deltaTime * MOVE_SPEED;

        Voxel_t5 voxel;
        Vector3 targetPoint, pivot;
        int key;

        //Conflict with OBSTACLE?
        //Check whether or not it touches an obstacle in 22.5 degree increments
        //from 90 degrees to the left of the input direction to 90 degrees to the right.
        for (int i = 0; i < inputRotation.Length; ++i)
        {
            targetPoint = position + (inputRotation[i] * inputDir * (delta + VOXEL_HALF_SIZE));
            pivot = Parser.GetVoxelPivot(targetPoint);
            key = Parser.GetVoxelIndex(pivot);
            if (map.TryGetValue(key, out voxel))
            {
                int idxMove = Parser.GetSubVoxelIndex(pivot, targetPoint);
                if (voxel.GetSubType(idxMove) == OBSTACLE)
                {
                    goto CHECK_OTHERS;
                }
            }
        }
        //If there is no collision area, go to "CHECK_SLOPE" without searching in another direction.
        goto CHECK_SLOPE;

    CHECK_OTHERS:
        //Search(1) Update inputDir if the last input direction is valid.
        //Search(2) Update inputDir if there is a valid direction among the 5 surrounding directions.
        Debug.Log("goto CHECK_OTHERS");
        return;

    CHECK_SLOPE:
        //If the "sub-voxel.Type" of the target location is SLOPE, calculate the y value.
        targetPoint = position + (inputDir * (delta + VOXEL_HALF_SIZE));
        pivot = Parser.GetVoxelPivot(targetPoint);
        key = Parser.GetVoxelIndex(pivot);
        if (map.TryGetValue(key, out voxel))
        {
            int idxMove = Parser.GetSubVoxelIndex(pivot, targetPoint);
            if (voxel.GetSubType(idxMove) >= SLOPE30)
            {
                //Update inputDir by reflecting "voxel.SlopeDirection" and "voxel.SlopeDegree" in the y value.
            }
        }

        //최종: 이동
        transform.position += delta * inputDir;
        Debug.Log("goto CHECK_SLOPE");
    }

    //public void Move(Dictionary<int, Voxel_t> voxel, Vector3 inputDir)
    //{
    //    Vector3 delta;
    //    Vector3 position = transform.position;

    //    int   current = GetDirectionIndex(inputDir);
    //    bool  isLeft  = CheckIsLeftDir(inputDir);
    //    int[] targets = GetTargetingIndexes(current, isLeft);

    //    inputDir.Normalize();
    //    delta = inputDir * MOVE_SPEED * Time.deltaTime;
    //    if (IsMovable(voxel, position, delta))
    //    {
    //        transform.position += delta;
    //        lastDelta = delta;
    //        lastIsLeft = inputDir.x < 0;
    //        return;
    //    }

    //    //2. ������ �Է� üũ
    //    delta = lastDelta;
    //    if (IsMovable(voxel, position, delta))
    //    {
    //        transform.position += delta;
    //        //lastDir = inputDir;
    //        //lastIsLeft = inputDir.x < 0;
    //        return;
    //    }

    //    //3. �ֺ� üũ
    //    delta = inputDir;
    //    for (int i = 0; i < targets.Length; ++i)
    //    {
    //        switch (targets[i])
    //        {
    //            case 0: delta = new Vector3(0f, 0f, 1f); break; // up
    //            case 1: delta = new Vector3(1f, 0f, 1f); break; // right up
    //            case 2: delta = new Vector3(1f, 0f, 0f); break; // right
    //            case 3: delta = new Vector3(1f, 0f, -1f); break; // right down
    //            case 4: delta = new Vector3(0f, 0f, -1f); break; // down
    //            case 5: delta = new Vector3(-1f, 0f, -1f); break; // left down
    //            case 6: delta = new Vector3(-1f, 0f, 0f); break; // left
    //            case 7: delta = new Vector3(-1f, 0f, 1f); break; // left up
    //        }

    //        delta *= MOVE_SPEED * Time.deltaTime;
    //        if (IsMovable(voxel, position, delta))
    //        {
    //            transform.position += delta;
    //            lastDelta = delta;

    //            if (inputDir.x != 0)
    //            {
    //                lastIsLeft = inputDir.x < 0;
    //            }
    //            return;
    //        }
    //    }
    //}
    //private bool IsMovable(Dictionary<int, Voxel_t> voxel,Vector3 pos, Vector3 delta)
    //{
    //    Debug.Log("Need New Parser");

    //    //Vector3 next = pos + delta;
    //    //Vector3 center = Parser.GetCenterPoint(next);
    //    //int radix = Parser.GetVoxelRadix(center);

    //    //if (voxel.ContainsKey(radix) 
    //    //    && Parser.GetVoxelType(voxel[radix], next - center) == VoxelType.Plain)
    //    //{
    //    //    return true;
    //    //}

    //    return false;
    //}
    //private int GetDirectionIndex(Vector3 dir)
    //{
    //    // not normalized: x �Ǵ� z ���� -1, 0, 1 �߿� �ϳ��δ�.
    //    float x = dir.x;
    //    float z = dir.z;

    //    //8���� ����� ��
    //    int current = -1;
    //    if (x == 0 && z > 0) { current = 0; }
    //    else if (x > 0 && z > 0) { current = 1; }
    //    else if (x > 0 && z == 0) { current = 2; }
    //    else if (x > 0 && z < 0) { current = 3; }
    //    else if (x == 0 && z < 0) { current = 4; }
    //    else if (x < 0 && z < 0) { current = 5; }
    //    else if (x < 0 && z == 0) { current = 6; }
    //    else if (x < 0 && z > 0) { current = 7; }

    //    Debug.Assert(current != -1, "Can`t Find Direction;");
    //    return current;
    //}
    //private bool CheckIsLeftDir(Vector3 inputDir)
    //{
    //    if (inputDir.x != 0)
    //    {
    //        return inputDir.x < 0;
    //    }

    //    return lastIsLeft;
    //}
    //private int[] GetTargetingIndexes(int directionIndex, bool isLeft)
    //{
    //    int[] result = new int[4] { -1, -1, -1, -1 };
    //    int interval = GetDirectionInterval(directionIndex, isLeft);

    //    result[0] = GetDirectionTargetIndex(directionIndex, interval);
    //    result[1] = GetDirectionTargetIndex(directionIndex, -interval);
    //    result[2] = GetDirectionTargetIndex(directionIndex, interval * 2);
    //    result[3] = GetDirectionTargetIndex(directionIndex, -interval * 2);

    //    return result;
    //}
    //private int GetDirectionInterval(int directionIndex, bool isLeft)
    //{
    //    switch (directionIndex)
    //    {
    //        case 0:
    //        case 1:
    //        case 2:
    //        case 6:
    //        case 7:
    //            if (isLeft) { return -1; }
    //            else { return 1; }

    //        case 3:
    //        case 4:
    //        case 5:
    //            if (isLeft) { return 1; }
    //            else { return -1; }
    //    }

    //    Debug.Assert(false, "Can`t Find Searching Direction;");
    //    return 0;
    //}
    //private int GetDirectionTargetIndex(int index, int interval)
    //{
    //    index += interval;
    //    if (index < 0)
    //    {
    //        index += 8;
    //    }

    //    return index % 8;
    //}
}
