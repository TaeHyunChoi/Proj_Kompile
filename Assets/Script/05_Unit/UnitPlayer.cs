using System.Collections.Generic;
using UnityEngine;
using static Public;
using PublicValue;
using UnityEngine.UIElements;
using Unity.VisualScripting;

public class UnitPlayer : Unit
{
    //Cached data
    private readonly Quaternion[] inputRotation = new Quaternion[9] 
    {
        Quaternion.AngleAxis(    0f, Vector3.up),
        Quaternion.AngleAxis(  -90f, Vector3.up),
        Quaternion.AngleAxis(  -45f, Vector3.up),
        Quaternion.AngleAxis(   45f, Vector3.up),
        Quaternion.AngleAxis(   90f, Vector3.up),

        Quaternion.AngleAxis(-22.5f, Vector3.up),
        Quaternion.AngleAxis( 22.5f, Vector3.up),
        Quaternion.AngleAxis(-67.5f, Vector3.up),
        Quaternion.AngleAxis( 67.5f, Vector3.up)
    };
    private bool hasLeftPriority;

    public void Move(Dictionary<int, Voxel_t5> map, Vector3 inputDir)
    {
        inputDir.Normalize();
        Vector3 position = transform.position;
        float delta = Time.deltaTime * MOVE_SPEED;

        //Conflict with OBSTACLE?
        int[] rotationPriorties = hasLeftPriority ? new int[5] { 0, 2, 1, 3, 4 } 
                                                  : new int[5] { 0, 3, 4, 2, 1 };

        for (int i = 0; i < rotationPriorties.Length; ++i)
        {
            int index = rotationPriorties[i];
            Vector3 rotatedDir = inputRotation[index] * inputDir;
            Vector3 targetPoint = position + (rotatedDir * (delta + VOXEL_HALF_SIZE));
            if (DoesNotCollided(map, targetPoint))
            {
                //If there is no collision area, go to "CHECK_SLOPE" without searching in another direction.
                inputDir = rotatedDir;
                goto CHECK_SLOPE;
            }
        }

    CHECK_SLOPE:
        Debug.Log("If the \"sub-voxel.Type\" of the target location is SLOPE, calculate the y value.");

        transform.position += delta * inputDir; //Move
        if      (inputDir.x > 0) { hasLeftPriority = true;  }
        else if (inputDir.x < 0) { hasLeftPriority = false; }
        //else { not update; }
    }
    private bool DoesNotCollided(Dictionary<int, Voxel_t5> map, Vector3 targetPoint)
    {
        //Check whether or not it touches an obstacle in 22.5 degree increments
        //from 90 degrees to the left of the input direction to 90 degrees to the right.

        for (int i = 0; i < inputRotation.Length; ++i)
        {
            Vector3 pivot = Parser.GetVoxelPivot(targetPoint);
            int key = Parser.GetVoxelIndex(pivot);
            if (map.TryGetValue(key, out Voxel_t5 voxel))
            {
                int idxSub = Parser.GetSubVoxelIndex(pivot, targetPoint);
                if (voxel.GetSubType(idxSub) == OBSTACLE)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
