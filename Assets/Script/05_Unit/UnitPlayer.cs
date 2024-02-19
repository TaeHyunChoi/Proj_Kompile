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

            if (!IsCollidedOrNull(map, colPoint1) && !IsCollidedOrNull(map, colPoint2))
            {
                inputDir = direction[i];
                goto MOVE;
            }

            Vector3 y = new Vector3(0f, VOXEL_SIZE - 0.001f, 0f);
            if (!IsCollidedOrNull(map, colPoint1 + y) && !IsCollidedOrNull(map, colPoint2 + y))
            {
                inputDir = direction[i];
                goto MOVE;
            }

            y = -y;
            if (!IsCollidedOrNull(map, colPoint1 + y) && !IsCollidedOrNull(map, colPoint2 + y))
            {
                inputDir = direction[i];
                goto MOVE;
            }
        }

        Debug.Log("Can`t move here. Move to oppsite side of last input?");
        return;



    MOVE:
        inputDir.Normalize();
        transform.position += delta * inputDir;

        //Is it necessary to find the y value at the ¡®current point (position without calculation)¡¯ rather than the position after addition?
        //++ Process as y = default_height + y_value.

        //Set Move Priority
        if (inputDir.x > 0) { hasRightPriority = true;  }
        else if (inputDir.x < 0) { hasRightPriority = false; }
        if      (inputDir.z > 0) { hasUpPriority    = true;  }
        else if (inputDir.z < 0) { hasUpPriority    = false; }
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
    }

    private bool IsCollidedOrNull(Dictionary<int, Voxel_t> map, Vector3 colPoint)
    {
        if (colPoint.x < 0 || colPoint.z < 0)
        {
            return false;
        }

        Vector3 pivot = Parser.GetVoxelPivot(colPoint);
        int key = Parser.GetVoxelKeyFromPivot(pivot);
        if (map.TryGetValue(key, out Voxel_t voxel))
        {
            int idxSub = Parser.GetSubVoxelIndex(pivot, colPoint);
            if (voxel.GetSubType(idxSub) == OBSTACLE)
            {
                return true; // is collided.
            }
        }
        else
        {
            return true; // null voxel;
        }

        return false;
    }

    //private void OnDrawGizmos()
    //{
    //    if (collisionPoints.Length == 0)
    //        return;

    //    Gizmos.color = Color.white;
    //    Gizmos.DrawLine(collisionPoints[0], collisionPoints[1]);
    //}
}