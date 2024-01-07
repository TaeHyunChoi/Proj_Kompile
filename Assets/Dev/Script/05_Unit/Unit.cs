using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    private Vector3 lastDirection;
    private bool isCollision;

    public void Move(Vector3 dir)
    {
        if(isCollision)
        {
            Debug.Log("IsCollision");
            //비비기 기능을 만들어야 한다...! 
            //콜라이더 던져서 이동 가능/불가 만드는 것도 좀 짜치는데 흠;
            return;
        }

        transform.position += dir * Public.MOVE_SPEED * Time.deltaTime;
        lastDirection = dir;
    }
    private void OnTriggerEnter(Collider other) 
    {
        isCollision = true;
    }
}
