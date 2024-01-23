using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Burst.Intrinsics;
using static Public;


public class Unit : MonoBehaviour
{
    private Vector3 lastDir;

    private void Awake()
    {
        lastDir = new Vector3(1f, 0, 1f);
    }

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
        int start = GetSearchStartingIndex(inputDir);

        //이거 rotate가 생각보다 어렵구먼? 3차원.. 저주하리...
        //이참에 생각 좀 해보자.
        int rotate;
        if (inputDir.z > 0) { rotate = 1; }
        else if (inputDir.z < 0) { rotate = -1; }
        else
        {
            if (lastDir.z > 0) { rotate = 1; }
            else { rotate = -1; }
        }

        if (lastDir.x > 0)
        {
            start += 2 * rotate;
            start %= 8;
        }
        else
        {
            start -= 2 * rotate;
            if (start < 0)
            {
                start = 8 + start;
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

            //좌우 증감 체크
            if (lastDir.x > 0)
            {
                index = (index - 1 * rotate) % 8;
                if (index < 0)
                {
                    index += 8;
                }
                if (index == start)
                {
                    index = (index - 1 * rotate) % 8;
                }
            }
            else
            {
                index = (index + 1 * rotate) % 8;
                if (index == start)
                {
                    index = (index + 1 * rotate) % 8;
                }
            }
        }
    }
    private int GetSearchStartingIndex(Vector3 dir)
    {
        // not normalized: x 또는 z 값이 -1, 0, 1 중에 하나로다.
        float x = dir.x;
        float z = dir.z;

        //8방향 경우의 수
        if (x == 0 && z > 0)    { return 0; }
        if (x > 0  && z > 0)    { return 1; }
        if (x > 0  && z == 0)   { return 2; }
        if (x > 0  && z < 0)    { return 3; }
        if (x == 0 && z < 0)    { return 4; }
        if (x < 0  && z < 0)    { return 5; }
        if (x == 0 && z < 0)    { return 6; }
        if (x < 0  && z > 0)    { return 7; }

        Debug.Assert(false, "Can`t Find Direction;");
        return -1;
    }
}
