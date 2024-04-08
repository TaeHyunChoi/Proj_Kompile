using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DataType;
using CMathf;
using static PTile;

public class UnitPlayer : UnitBase
{
    private readonly int[] intervalRot = new int[] { 0, 1, -1, 2, -2 }; //시계 방향을 우선 탐색하는 기준
    private readonly float SPEED_MOVE = 4f;

    private Vector3 dirBefore;

    public void Move(Dictionary<int, Tile_t> map, Vector3 dirInput, float scale)
    {
        Vector3 pointNow = transform.position;
        if (false == map.TryGetValue(GetKey(pointNow, scale), out Tile_t tileNow))
        {
            Debug.LogAssertion("Impossible position " + pointNow);
            return;
        }

        Vector3 dir;
        scale = tileNow.Scale;
        int keyNow = PTile.GetKey(pointNow, scale);
        float dist = CMath.Floor(Time.deltaTime * SPEED_MOVE, 3);


        //CHECK_INPUT_DIR:
        for (int c = 0; c < 3; ++c)
        {
            Vector3 dirCollide = Quaternion.Euler(0f, intervalRot[c] * 45f, 0f) * dirInput;
            dirCollide.Normalize();
            dirCollide = CMath.FloorToVector(dirInput * (SIZE_QUATER + dist), 3);

            Vector3 pointCollide = CMath.FloorToVector(pointNow + dirCollide, 3);
            int keyCollide = PTile.GetKey(pointCollide, scale);


            //같은 타일 + 해당 분면으로 이동 불가하다면 다른 타일 탐색
            if (keyCollide == keyNow)
            {
                if (false == tileNow.IsMovable(keyNow, pointCollide))
                {
                    goto CHECK_OTHER_DIRS;
                }
            }

            //다른 타일로 이동 가능? link 여부 확인
            else if (true == map.ContainsKey(keyCollide))
            {
                if (false == tileNow.IsLinked(keyNow, pointCollide))
                {
                    goto CHECK_OTHER_DIRS;
                }
            }

            //다른 타일로 이동도 불가능? 다른 타일 알아봐라.
            else
            {
                goto CHECK_OTHER_DIRS;
            }
        }

        goto SET_POSITION;


    CHECK_OTHER_DIRS:
        float rotY = Mathf.Sign(Vector3.Cross(dirInput, dirBefore).y);
        Debug.Log("CHECK_OTHER_DIRS");
        return;

    SET_POSITION:
        dirInput.Normalize();
        dir = CMath.FloorToVector(dirInput * dist, 3);
        transform.position = CMath.FloorToVector(pointNow + dir, 3);
        dirBefore = dir;
    }
}
