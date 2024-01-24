using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Public;

public class UnitPlayer : Unit
{
    private Vector3 lastDir;

    public void Move(Dictionary<int, Voxel_t> voxel, Vector3 inputDir)
    {
        int current = GetDirectionIndex(inputDir);
        bool isLeft = CheckIsLeftDir(inputDir);
        int[] targets = GetTargetingIndexes(current, isLeft);

        for (int i = 0; i < targets.Length; ++i)
        {
            switch (targets[i])
            {
                case 0: inputDir = new Vector3(0f, 0f, 1f); break; // up
                case 1: inputDir = new Vector3(1f, 0f, 1f); break; // right up
                case 2: inputDir = new Vector3(1f, 0f, 0f); break; // right
                case 3: inputDir = new Vector3(1f, 0f, -1f); break; // right down
                case 4: inputDir = new Vector3(0f, 0f, -1f); break; // down
                case 5: inputDir = new Vector3(-1f, 0f, -1f); break; // left down
                case 6: inputDir = new Vector3(-1f, 0f, 0f); break; // left
                case 7: inputDir = new Vector3(-1f, 0f, 1f); break; // left up
            }

            inputDir.Normalize();
            Vector3 next = transform.position + inputDir * (HALF_GRID_SIZE + float.Epsilon);
            Vector3 center = Parser.GetCenterPoint(next);
            int radix = Parser.GetVoxelRadix(center);

            if (voxel.ContainsKey(radix)
                && Parser.GetVoxelType(voxel[radix], next - center) == VoxelType.Movable)
            {
                transform.position += inputDir * MOVE_SPEED * Time.deltaTime;

                float x = inputDir.x != 0 ? inputDir.x : lastDir.x;
                float z = inputDir.z != 0 ? inputDir.z : lastDir.z;
                lastDir = new Vector3(x, 0, z);

                break;
            }
        }
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

        return lastDir.x < 0;
    }
    private int[] GetTargetingIndexes(int directionIndex, bool isLeft)
    {
        int[] result = new int[5] { directionIndex, -1, -1, -1, -1 };
        int interval = GetDirectionInterval(directionIndex, isLeft);

        result[1] = GetDirectionTargetIndex(directionIndex, interval);
        result[2] = GetDirectionTargetIndex(directionIndex, -interval);
        result[3] = GetDirectionTargetIndex(directionIndex, interval * 2);
        result[4] = GetDirectionTargetIndex(directionIndex, -interval * 2);

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
