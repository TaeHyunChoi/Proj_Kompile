using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    private Vector3 lastDirection;

    public void Move(Dictionary<int, byte> voxel, Vector3 dir)
    {
        Vector3 next = transform.position + dir * Public.MOVE_SPEED * Time.deltaTime;
        if (Parser.IsMovePossible(voxel, next))
        {
            transform.position = next;
            lastDirection = dir;
        }
    }
}
