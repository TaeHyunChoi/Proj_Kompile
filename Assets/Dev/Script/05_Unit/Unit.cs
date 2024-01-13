using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    private Vector3 lastDirection;

    //마지막 입력방향에 따라 탐색 순서가 달라져야 하는군?
    //좌: {정:좌:우:후} - {상정:상좌:상우:상후} - {하정:하좌:하우:하후}
    //우: {정:우:좌:후} - {상정:상우:상좌:상후} - {하정:하우:하좌:하후}
    private static int[][] arr = new int[2][] {
        new int[] {1,2,3,4,5},
        new int[] {5,4,3,2,1}
        };

    public void Move(Dictionary<int, int> voxel, Vector3 dir)
    {
        float delta = Public.MOVE_SPEED * Time.deltaTime;
        Vector3 next = transform.position + dir * delta;

        //정방향
        if (Parser.IsMovePossible(voxel, next))
        {
            transform.position = next;
            lastDirection = dir;
            return;
        }
        //   좌
        next = transform.position + (-transform.right) * delta;
        if (Parser.IsMovePossible(voxel, next))
        {
            transform.position = next;
            lastDirection = dir;
            return;
        }
        //우
        //후

        //상+중
        //상+좌
        //상+우
        //상+후

        //하+중
        //하+좌
        //하+우
        //하+후
    }
}
