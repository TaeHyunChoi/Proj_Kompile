using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Burst.Intrinsics;
using static Public;


public class Unit : MonoBehaviour
{
    private Vector3 lastDir;

    //Player로 떼어내야지 이거...
    public void Move(Dictionary<int, Voxel_t> voxel, Vector3 inputDir)
    {
        Vector3 next = transform.position + (inputDir.normalized * MOVE_SPEED * Time.deltaTime);
        Vector3 center = Parser.GetCenterPoint(next);
        int radix = Parser.GetVoxelRadix(center);

        //// check data
        if (!voxel.ContainsKey(radix))
        {
            return;
        }

        //// target voxel
        if (Parser.GetVoxelType(voxel[radix], next - center) == VoxelType.Movable)
        {
            transform.position += (inputDir.normalized * MOVE_SPEED * Time.deltaTime);

            float x = (inputDir.x != 0) ? inputDir.x : lastDir.x;
            float z = (inputDir.z != 0) ? inputDir.z : lastDir.z;
            lastDir = new Vector3(x, 0, z);

            return;
        }

        //// neighbor voxels
        int start = GetCurrentDirection(inputDir);
        int rotate;

        if (inputDir.x != 0 && inputDir.z == 0)
        {
            if (lastDir.z > 0) { rotate = 1; }
            else { rotate = -1; }
        }
        else if (inputDir.x == 0 & inputDir.z != 0)
        {
            if (lastDir.x > 0) { rotate = 1; }
            else { rotate = -1; }
        }
        //inputDir.x 와 .z 모두 0이면 입력이 없으므로 ㄴㄴ
        //최초라면 그대로 입력값을 사용
        else
        {
            if (inputDir.x > 0 || inputDir.z > 0)
            {
                rotate = 1;
            }
            else
            {
                rotate = -1;
            }
        }

        if (inputDir.z < 0)
        {
            rotate *= -1;
        }
        if (rotate == 1)
        {
            start = (start + 2) % 8;
        }
        else
        {
            start -= 2;
            if (start < 0)
            {
                start += 8;
            }
        }

        int index = start;
        int count = 0;

        while (++count <= 5)
        {
            switch (index)
            {
                case 0: inputDir = new Vector3( 0f, 0f,  1f);   break; // up
                case 1: inputDir = new Vector3( 1f, 0f,  1f);   break; // right up
                case 2: inputDir = new Vector3( 1f, 0f,  0f);   break; // right
                case 3: inputDir = new Vector3( 1f, 0f, -1f);   break; // right down
                case 4: inputDir = new Vector3( 0f, 0f, -1f);   break; // down
                case 5: inputDir = new Vector3(-1f, 0f, -1f);   break; // left down
                case 6: inputDir = new Vector3(-1f, 0f,  0f);   break; // left
                case 7: inputDir = new Vector3(-1f, 0f,  1f);   break; // left up
            }

            inputDir.Normalize();

            //선구현 후정리 ㄱㄱ
            next = transform.position + (inputDir.normalized * MOVE_SPEED * Time.deltaTime);
            center = Parser.GetCenterPoint(next);
            radix = Parser.GetVoxelRadix(center);

            //// check data
            if (voxel.ContainsKey(radix)
                && Parser.GetVoxelType(voxel[radix], next - center) == VoxelType.Movable)
            {
                transform.position += (inputDir.normalized * MOVE_SPEED * Time.deltaTime);

                float x = (inputDir.x != 0) ? inputDir.x : lastDir.x;
                float z = (inputDir.z != 0) ? inputDir.z : lastDir.z;
                lastDir = new Vector3(x, 0, z);

                Debug.Log($"[{start}->{index}] dir:{inputDir}");
                return;
            }

            index -= rotate;
            if (index < 0)
            {
                index = 8 + index;
            }
            else
            {
                index %= 8;
            }
        }
    }
    private int GetCurrentDirection(Vector3 dir)
    {
        // not normalized: x 또는 z 값이 -1, 0, 1 중에 하나로다.
        float x = dir.x;
        float z = dir.z;

        //8방향 경우의 수
        int current = -1;
        if (x == 0 && z > 0)    { current = 0; }
        else if (x > 0  && z > 0)    { current = 1; }
        else if (x > 0  && z == 0)   { current = 2; }
        else if (x > 0  && z < 0)    { current = 3; }
        else if (x == 0 && z < 0)    { current = 4; }
        else if (x < 0  && z < 0)    { current = 5; }
        else if (x < 0 && z == 0)    { current = 6; }
        else if (x < 0  && z > 0)    { current = 7; }

        Debug.Assert(current != -1, "Can`t Find Direction;");
        return current;
    }
}
