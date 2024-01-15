using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Burst.Intrinsics;
using static Public;


public class Unit : MonoBehaviour
{
    public void Move(Dictionary<int, Voxel_t> voxel, Vector3 inputDir)
    {
        //실험 중이니까 여기에 코드 작성 중 >> 스크립트 분리해야 한답;

        float delta = Public.MOVE_SPEED * Time.deltaTime;
        int index = 0;

        Vector3 voxelPoint = Parser.GetVoxelPoint(transform.position);
        Vector3 neighbor = voxelPoint;
        Vector3 pointDir = inputDir;

        //1. 입력한 방향으로 다음 복셀로 이동 가능한지 판단 (이동 방향 면)
        while (index++ < 8)
        {
            switch (index)
            {
                case 0: pointDir = inputDir; break;  //origin Input Direction
                case 1: pointDir = inputDir + new Vector3(inputDir.x, 0f, 0f); break;  //origin + x, 원래 입력한 x축 방향으로 이어서
                case 2: pointDir = inputDir + new Vector3(-inputDir.x, 0f, 0f); break;  //origin - x


            }
        }

        neighbor = voxelPoint + pointDir * Public.VOXEL_SIZE;
        if (Parser.IsMovable(voxel, neighbor, neighbor - voxelPoint))
        {
            goto Move;
        }

        //2. 이동 방향 면으로 진입이 불가하다면 본인의 좌/우 면을 판단


        //3. 본인의 좌우도 진입이 불가하다면 입력한 방향의 반대 방향으로 다음 복셀 이동이 가능한지 판단



        while (index++ < 24)
        {
            switch(index)
            {
                //dir을 기준으로 variation을 칠 수 있나?
                case 0:
                    neighbor = voxelPoint + inputDir * Public.VOXEL_SIZE;
                    break;
                case 1:
                    break;
                case 2:
                    break;
                default:
                    Debug.LogError("Can`t Move.");
                    return; //이동 불가;
            }

            inputDir = neighbor - voxelPoint;
            if (Parser.IsMovable(voxel, neighbor, inputDir))
            {
                goto Move;
            }
        }



        neighbor = pivot + new Vector3(inputDir.x, 0, 0) * Public.VOXEL_SIZE;
        inputDir = neighbor - voxelPoint;
        if (Parser.IsMovable(voxel, neighbor, inputDir))
        {
            goto Move;
        }

        neighbor = pivot - new Vector3(inputDir.x, 0, 0) * Public.VOXEL_SIZE;
        inputDir = neighbor - voxelPoint;
        if (Parser.IsMovable(voxel, neighbor, inputDir))
        {
            goto Move;
        }
        else
        {
            //어디에도 해당 안되면 dir = 0;
            inputDir = Vector3.zero;   
        }


        Move:
        transform.position += inputDir.normalized * delta;
    }
}
