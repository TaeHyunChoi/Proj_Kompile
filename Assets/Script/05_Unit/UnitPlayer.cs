using System.Collections.Generic;
using UnityEngine;
using static Public;

public class UnitPlayer : Unit
{
    private Vector3 lastDelta;
    private bool lastIsLeft;

    public void Move(Dictionary<int, Voxel_t> voxel, Vector3 inputDir)
    {
        Vector3 delta;
        Vector3 position = transform.position;

        int   current = GetDirectionIndex(inputDir);
        bool  isLeft  = CheckIsLeftDir(inputDir);
        int[] targets = GetTargetingIndexes(current, isLeft);

        //1. 최초 입력 체크
        inputDir.Normalize();
        delta = inputDir * MOVE_SPEED * Time.deltaTime;
        if (IsMovable(voxel, position, delta))
        {
            transform.position += delta;
            lastDelta = delta;
            lastIsLeft = inputDir.x < 0;
            return;
        }

        //2. 마지막 입력 체크
        delta = lastDelta;
        if (IsMovable(voxel, position, delta))
        {
            transform.position += delta;
            //lastDir = inputDir;
            //lastIsLeft = inputDir.x < 0;
            return;
        }

        //3. 주변 체크
        delta = inputDir;
        for (int i = 0; i < targets.Length; ++i)
        {
            switch (targets[i])
            {
                case 0: delta = new Vector3(0f, 0f, 1f); break; // up
                case 1: delta = new Vector3(1f, 0f, 1f); break; // right up
                case 2: delta = new Vector3(1f, 0f, 0f); break; // right
                case 3: delta = new Vector3(1f, 0f, -1f); break; // right down
                case 4: delta = new Vector3(0f, 0f, -1f); break; // down
                case 5: delta = new Vector3(-1f, 0f, -1f); break; // left down
                case 6: delta = new Vector3(-1f, 0f, 0f); break; // left
                case 7: delta = new Vector3(-1f, 0f, 1f); break; // left up
            }

            delta *= MOVE_SPEED * Time.deltaTime;
            if (IsMovable(voxel, position, delta))
            {
                transform.position += delta;
                lastDelta = delta;

                if (inputDir.x != 0)
                {
                    lastIsLeft = inputDir.x < 0;
                }
                return;
            }
        }
    }
    private bool IsMovable(Dictionary<int, Voxel_t> voxel,Vector3 pos, Vector3 delta)
    {
        Vector3 next = pos + delta;
        Vector3 center = Parser.GetCenterPoint(next);
        int radix = Parser.GetVoxelRadix(center);

        if (voxel.ContainsKey(radix) 
            && Parser.GetVoxelType(voxel[radix], next - center) == SubVoxelType.Plain)
        {
            return true;
        }

        return false;
    }

    private int GetDirectionIndex(Vector3 dir)
    {
        // not normalized: x 또는 z 값이 -1, 0, 1 중에 하나로다.
        float x = dir.x;
        float z = dir.z;

        //8방향 경우의 수
        int current = -1;
        if (x == 0 && z > 0) { current = 0; }
        else if (x > 0 && z > 0) { current = 1; }
        else if (x > 0 && z == 0) { current = 2; }
        else if (x > 0 && z < 0) { current = 3; }
        else if (x == 0 && z < 0) { current = 4; }
        else if (x < 0 && z < 0) { current = 5; }
        else if (x < 0 && z == 0) { current = 6; }
        else if (x < 0 && z > 0) { current = 7; }

        Debug.Assert(current != -1, "Can`t Find Direction;");
        return current;
    }
    private bool CheckIsLeftDir(Vector3 inputDir)
    {
        if (inputDir.x != 0)
        {
            return inputDir.x < 0;
        }

        return lastIsLeft;
    }
    private int[] GetTargetingIndexes(int directionIndex, bool isLeft)
    {
        int[] result = new int[4] { -1, -1, -1, -1 };
        int interval = GetDirectionInterval(directionIndex, isLeft);

        result[0] = GetDirectionTargetIndex(directionIndex, interval);
        result[1] = GetDirectionTargetIndex(directionIndex, -interval);
        result[2] = GetDirectionTargetIndex(directionIndex, interval * 2);
        result[3] = GetDirectionTargetIndex(directionIndex, -interval * 2);

        return result;
    }
    private int GetDirectionInterval(int directionIndex, bool isLeft)
    {
        switch (directionIndex)
        {
            case 0:
            case 1:
            case 2:
            case 6:
            case 7:
                if (isLeft) { return -1; }
                else { return 1; }

            case 3:
            case 4:
            case 5:
                if (isLeft) { return 1; }
                else { return -1; }
        }

        Debug.Assert(false, "Can`t Find Searching Direction;");
        return 0;
    }
    private int GetDirectionTargetIndex(int index, int interval)
    {
        index += interval;
        if (index < 0)
        {
            index += 8;
        }

        return index % 8;
    }
}
