using System.Collections.Generic;
using UnityEngine;
using static Public;
using CDataStructure;
using CMathf;

public class UnitPlayer : Unit
{
    //Take octagon points and partially check collisions according to direction and decision priority. (see #region below)
    private readonly Vector3[] cacheDirection = new Vector3[]
    {
        new Vector3( VOXEL_HALF_SIZE       ,  0f,  0f                    ).normalized,
        new Vector3( VOXEL_HALF_SIZE       ,  0f,  VOXEL_HALF_SIZE * 0.5f).normalized,
        new Vector3( VOXEL_HALF_SIZE       ,  0f,  VOXEL_HALF_SIZE       ).normalized,
        new Vector3( VOXEL_HALF_SIZE * 0.5f,  0f,  VOXEL_HALF_SIZE       ).normalized,

        new Vector3( 0f                    ,  0f,  VOXEL_HALF_SIZE       ).normalized,
        new Vector3(-VOXEL_HALF_SIZE * 0.5f,  0f,  VOXEL_HALF_SIZE       ).normalized,
        new Vector3(-VOXEL_HALF_SIZE       ,  0f,  VOXEL_HALF_SIZE       ).normalized,
        new Vector3(-VOXEL_HALF_SIZE       ,  0f,  VOXEL_HALF_SIZE * 0.5f).normalized,

        new Vector3(-VOXEL_HALF_SIZE       ,  0f,  0f                    ).normalized,
        new Vector3(-VOXEL_HALF_SIZE       ,  0f, -VOXEL_HALF_SIZE * 0.5f).normalized,
        new Vector3(-VOXEL_HALF_SIZE       ,  0f, -VOXEL_HALF_SIZE       ).normalized,
        new Vector3(-VOXEL_HALF_SIZE * 0.5f,  0f, -VOXEL_HALF_SIZE       ).normalized,

        new Vector3( 0f                    ,  0f, -VOXEL_HALF_SIZE       ).normalized,
        new Vector3( VOXEL_HALF_SIZE * 0.5f,  0f, -VOXEL_HALF_SIZE       ).normalized,
        new Vector3( VOXEL_HALF_SIZE       ,  0f, -VOXEL_HALF_SIZE       ).normalized,
        new Vector3( VOXEL_HALF_SIZE       ,  0f, -VOXEL_HALF_SIZE * 0.5f).normalized
    };
    private Vector3[] direction               = new Vector3[5];

    private bool hasRightPriority;
    private bool hasUpPriority;

    public void Move(Dictionary<int, Voxel_t> map, Vector3 inputDir)
    {
        inputDir.Normalize();
        Vector3 position = CMath.Floor1000Vector3(transform.position);
        float delta = Time.deltaTime * MOVE_SPEED;

        //Conflict with OBSTACLE?
        int idxDir = GetDirectionIndex(inputDir);
        GetTargetDirections(idxDir);

        for (int i = 0; i < direction.Length; ++i)
        {
            int     idxTarget = GetDirectionIndex(direction[i]);
            Vector3 colPoint1 = CMath.Floor1000Vector3(position + cacheDirection[(idxTarget + 1) % 16]      * (delta + VOXEL_HALF_SIZE));
            Vector3 colPoint2 = CMath.Floor1000Vector3(position + cacheDirection[(idxTarget - 1 + 16) % 16] * (delta + VOXEL_HALF_SIZE));

            if (DoesNotCollided(map, colPoint1) && DoesNotCollided(map, colPoint2))
            {
                inputDir = direction[i];
                goto MOVE;
            }
        }

        Debug.Log("Can`t move here. Move to oppsite side of last input?");
        return;

    MOVE:
        transform.position += delta * inputDir;

        //Set Move Priority
        if      (inputDir.x > 0) { hasRightPriority = true;  }
        else if (inputDir.x < 0) { hasRightPriority = false; }
        if      (inputDir.z > 0) { hasUpPriority = true; }
        else if (inputDir.z < 0) { hasUpPriority = false; }
    }
    private int GetDirectionIndex(Vector3 inputDir)
    {
        int index = 0;
        
        if      (inputDir.x > 0) { index += 0100; }
        else if (inputDir.x < 0) { index += 1100; }
        if      (inputDir.z > 0) { index += 0001; }
        else if (inputDir.z < 0) { index += 0011; }

        switch (index)
        {
            case 0100: index =  0; break; // ( 1,  0)
            case 0101: index =  2; break; // ( 1,  1)
            case 0001: index =  4; break; // ( 0,  1)
            case 1101: index =  6; break; // (-1,  1)  
            case 1100: index =  8; break; // (-1,  0) 
            case 1111: index = 10; break; // (-1, -1)  
            case 0011: index = 12; break; // ( 0, -1)
            case 0111: index = 14; break; // ( 1, -1)
        }

        return index;
    }
    private void GetTargetDirections(int index)
    {
        int option = 0;

        switch (index)
        {
            //z is zero
            case 0: //( 1,  0)
                option = hasUpPriority ? 1 : 2;
                break; 
            case 8: //(-1,  0)
                option = hasUpPriority ? 2 : 1; 
                break; 

            //z upper
            case 2: //( 1,  1)
            case 4: //( 0,  1)
            case 6: //(-1,  1)
                    option = hasRightPriority ? 2 : 1;
                break;

            //z lower
            case 10: //(-1, -1)
            case 12: //( 0, -1)
            case 14: //( 1, -1)
                option = hasRightPriority ? 1 : 2;
                break;
        }

        direction[0] = cacheDirection[index];
        if (option == 1)
        {
            direction[1] = cacheDirection[(index + 2) % 16];
            direction[2] = cacheDirection[(index + 4) % 16];
            direction[3] = cacheDirection[(index - 2 + 16) % 16];
            direction[4] = cacheDirection[(index - 4 + 16) % 16];
        }
        else if (option == 2)
        {
            direction[1] = cacheDirection[(index - 2 + 16) % 16];
            direction[2] = cacheDirection[(index - 4 + 16) % 16];
            direction[3] = cacheDirection[(index + 2) % 16];
            direction[4] = cacheDirection[(index + 4) % 16];
        }
        else
        {
            Debug.LogError("option?");
        }
    }

    //I don't want to use heap memory, so I pass it as a parameter. (Close your eyes on code duplication...)
    private bool DoesNotCollided(Dictionary<int, Voxel_t> map, Vector3 colPoint)
    {
        if (colPoint.x < 0 || colPoint.z < 0)
        {
            return false;
        }

        Vector3 pivot = Parser.GetVoxelPivot(colPoint);
        int key = Parser.GetVoxelIndex(pivot);
        if (map.TryGetValue(key, out Voxel_t voxel1))
        {
            int idxSub = Parser.GetSubVoxelIndex(pivot, colPoint);
            if (voxel1.GetSubType(idxSub) == OBSTACLE)
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        return true;
    }

    //private void OnDrawGizmos()
    //{
    //    if (collisionPoints.Length == 0)
    //        return;

    //    Gizmos.color = Color.white;
    //    Gizmos.DrawLine(collisionPoints[0], collisionPoints[1]);
    //}


    #region [Not Used] Vector rotation was used, but it cannot be used because there is a problem with the diagonal movement distance attached to the wall.
    /*
    private int[] collisionPoints = new int[4];
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
    private void GetRotationPriority(Vector3 direction, out int[] priority)
    {
        int value = 0;
        if      (direction.x > 0) { value += 0100; }
        else if (direction.x < 0) { value += 1100; }
        if      (direction.z > 0) { value += 0001; }
        else if (direction.z < 0) { value += 0011; }

        switch (value)
        {
            case 0100: //( 1,  0)
                value = hasUpPriority ? 0 : 1;
                break;
            case 1100: //(-1,  0)
                value = hasUpPriority ? 1 : 0;
                break;
            case 0101: //( 1,  1) 
            case 0001: //( 0,  1)
            case 1101: //(-1,  1)
                value = hasRightPriority ? 1 : 0;
                break;
            case 1111: //(-1, -1)
            case 0011: //( 0, -1)
            case 0111: //( 1, -1)
                value = hasRightPriority ? 0 : 1;
                break;
        }

        if  (value == 0) { priority = new int[5] { 0, 2, 1, 3, 4 }; }
        else             { priority = new int[5] { 0, 3, 4, 2, 1 }; }
    }
    private bool DoesNotCollided(Dictionary<int, Voxel_t5> map, Vector3 position, Vector3 dir, float delta)
    {
        //Check whether or not it touches an obstacle in 22.5 degree increments
        //from 90 degrees to the left of the input direction to 90 degrees to the right.

        for (int i = 0; i < inputRotation.Length; ++i)
        {
            Vector3 targetPoint = position + (inputRotation[i] * dir) * (delta + VOXEL_HALF_SIZE);
            Vector3 pivot = Parser.GetVoxelPivot(targetPoint);
            int key = Parser.GetVoxelIndex(pivot);

            if (map.TryGetValue(key, out Voxel_t5 voxel))
            {
                int idxSub = Parser.GetSubVoxelIndex(pivot, targetPoint);
                if (voxel.GetSubType(idxSub) == OBSTACLE)
                {
                    Debug.Log($"[OBSTACLE][{i}] {position}=>{targetPoint}\n\t{pivot}.move[{idxSub}] == {System.Convert.ToString(voxel.Move, 2)}");
                    return false;
                }
                else
                {
                    Debug.Log($"[MOVABLE][{i}] {position}=>{targetPoint}\n\t{pivot}.move[{idxSub}] == {System.Convert.ToString(voxel.Move, 2)}");
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }
    //*/
    #endregion
}
