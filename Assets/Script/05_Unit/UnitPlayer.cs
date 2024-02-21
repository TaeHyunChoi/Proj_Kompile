using System.Collections.Generic;
using UnityEngine;
using static Public;
using CDataStructure;
using CMathf;

/// <summary>
/// It would be better to attach it as a "movement operation" component class in the future. The size is quite large;
/// </summary>
public class UnitPlayer : Unit
{
    //Take octagon points and partially check collisions according to direction and decision priority. (see #region below)
    private readonly Vector3[] cacheDirection = new Vector3[]
    {
        new Vector3(   1f,    0f,    0f).normalized,
        new Vector3(   1f,    0f,  0.5f).normalized,
        new Vector3(   1f,    0f,    1f).normalized,
        new Vector3( 0.5f,    0f,    1f).normalized,

        new Vector3(   0f,    0f,    1f).normalized,
        new Vector3(-0.5f,    0f,    1f).normalized,
        new Vector3(  -1f,    0f,    1f).normalized,
        new Vector3(  -1f,    0f,  0.5f).normalized,

        new Vector3(  -1f,    0f,    0f).normalized,
        new Vector3(  -1f,    0f, -0.5f).normalized,
        new Vector3(  -1f,    0f,   -1f).normalized,
        new Vector3(-0.5f,    0f,   -1f).normalized,

        new Vector3(   0f,    0f,   -1f).normalized,
        new Vector3( 0.5f,    0f,   -1f).normalized,
        new Vector3(   1f,    0f,   -1f).normalized,
        new Vector3(   1f,    0f, -0.5f).normalized
    };
    private Vector3[] direction               = new Vector3[5];
    private Vector3 colPoint1, colPoint2;

    private bool hasRightPriority;
    private bool hasUpPriority;

    public void Move(Dictionary<int, Voxel_t> map, Vector3 inputDir)
    {
        //data
        Vector3 currentPos = transform.position;
        inputDir.Normalize();
        float delta = Time.deltaTime * MOVE_SPEED;

        //is collided or null? -> Get Direction
        direction = GetTargetDirections(inputDir);
        for (int i = 0; i < direction.Length; ++i)
        {
            int idxDir = GetDirectionIndex(direction[i]);
            colPoint1 = CMath.Floor1000Vector3(currentPos + cacheDirection[(idxDir + 1) % 16]      * (delta + VOXEL_HALF_SIZE));
            colPoint2 = CMath.Floor1000Vector3(currentPos + cacheDirection[(idxDir - 1 + 16) % 16] * (delta + VOXEL_HALF_SIZE));

            //Navigating y value order :  0f,  1f, -1f
            for (int j = 0; j < 3; ++j)
            {
                float dirY = (j + 1) % 3 - 1;
                Vector3 offset = new Vector3(0f, dirY * (VOXEL_HALF_SIZE + 0.001f), 0f);

                if (IsMovable(map, colPoint1 + offset) && IsMovable(map, colPoint2 + offset))
                {
                    inputDir = direction[i];
                    inputDir.Normalize();
                    inputDir *= delta;

                    if (   true == TryGetNextPosition(map, currentPos,          inputDir, out Vector3 nextPos)
                        || true == TryGetNextPosition(map, currentPos + offset, inputDir, out         nextPos))
                    {
                        //Move
                        transform.position = CMath.Floor1000Vector3(nextPos);

                        //Set move direction priority.
                        if      (inputDir.x > 0) { hasRightPriority = true;  }
                        else if (inputDir.x < 0) { hasRightPriority = false; }
                        if      (inputDir.z > 0) { hasUpPriority    = true;  }
                        else if (inputDir.z < 0) { hasUpPriority    = false; }

                        return;
                    }
                }
            }
        }
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
    private Vector3[] GetTargetDirections(Vector3 inputDir)
    {
        int index = GetDirectionIndex(inputDir);
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
            default:
#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
                Debug.LogError("Wierd Direction;");
#endif
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

        return direction;
    }

    private bool IsMovable(Dictionary<int, Voxel_t> map, Vector3 colPoint)
    {
        if (colPoint.x < 0 || colPoint.z < 0)
        {
            return false; // null voxel (out of range).
        }

        Vector3 pivot = Parser.GetVoxelPivot(colPoint);
        int key = Parser.GetVoxelKeyFromPivot(pivot);
        if (map.TryGetValue(key, out Voxel_t voxel))
        {
            int idxSub = Parser.GetSubVoxelIndex(pivot, colPoint);
            if (voxel.GetSubType(idxSub) == OBSTACLE)
            {
                return false; // is collided.
            }
        }
        else
        {
            return false; // null voxel.
        }

        return true;
    }
    private bool TryGetNextPosition(Dictionary<int, Voxel_t> map, Vector3 currentPos, Vector3 dir, out Vector3 nextPos)
    {
        nextPos = currentPos + dir;
        Vector3 nextPivot = Parser.GetVoxelPivot(nextPos);
        int key = Parser.GetVoxelKeyFromPivot(nextPivot);

        if (map.TryGetValue(key, out Voxel_t targetVoxel))
        {
            int idxSub = Parser.GetSubVoxelIndex(nextPivot, nextPos);
            idxSub = targetVoxel.GetSubType(idxSub);

            //TODO: add condition: can`t move between different height voxels.

            if (idxSub != OBSTACLE)
            {
                //if targeted voxel has slope, set the y value of the targeted coordinates.
                if (targetVoxel.SlopeFlag != 0)
                {
                    Vector3 lowPoint = nextPivot + (new Vector3(1f, 0f, 1f) - targetVoxel.SlopeDirection) * VOXEL_HALF_SIZE;
                    float size = Vector3.Distance(lowPoint, nextPos);
                    float angle = Vector3.Angle(from: targetVoxel.SlopeDirection, to: nextPos - lowPoint);
                    angle = (angle + 180) % 180; //unsigned

                    nextPos = new Vector3(nextPos.x, CMath.Floor1000(nextPivot.y + (Mathf.Cos(angle * Mathf.Deg2Rad) * size)), nextPos.z);
                }
                else
                {
                    nextPos = new Vector3(nextPos.x, nextPivot.y, nextPos.z);
                }

                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, VOXEL_HALF_SIZE);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(colPoint1, colPoint2);
        Vector3 y = new Vector3(0f, VOXEL_HALF_SIZE - 0.001f, 0f);
        Gizmos.DrawLine(colPoint1, colPoint1 - y);
        Gizmos.DrawLine(colPoint2, colPoint2 - y);
        Gizmos.DrawLine(colPoint1, colPoint1 + y);
        Gizmos.DrawLine(colPoint2, colPoint2 + y);
    }
}