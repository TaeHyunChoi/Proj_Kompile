using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DataType;
using CMathf;
using static PTile;

public class UnitPlayer : UnitBase
{
    private readonly int[] intervalRot = new int[] { 0, 1, -1, 2, -2 }; //시계 방향을 우선 탐색하는 기준
    private readonly float SPEED_MOVE = 1f;

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

            //최소, 최대 범위 안에 있는가
            if (false == PTile.IsInGrid(pointCollide.x, pointCollide.z))
            {
                goto CHECK_OTHER_DIRS;
            }

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

        //목적지 타일 정보를 가져와서 + y값을 꺼내야 한다.
        Vector3 pointGoal = CMath.FloorToVector(pointNow + dir, 3);
        int keyGoal = PTile.GetKey(pointGoal, scale);

        if (false == map.TryGetValue(keyGoal, out Tile_t tileGoal))
        {
            Debug.LogError("No Tile goal position");
            return;
        }

        float y = tileGoal.GetYValue(keyGoal, pointGoal);
        transform.position = new Vector3(pointGoal.x, y, pointGoal.z);
        //transform.position = CMath.FloorToVector(pointNow + dir, 3);
        dirBefore = dir;
    }
}
