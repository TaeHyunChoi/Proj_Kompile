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

    private float sign;
    private bool hasRightPriority;
    private bool hasUpPriority;

    private Vector3 colPoint1, colPoint2;

    public void Move(Dictionary<int, Voxel_t> map, Vector3 inputDir)
    {
        Vector3 position = CMath.Floor1000Vector3(transform.position);
        float   delta = Time.deltaTime * MOVE_SPEED;
        inputDir.Normalize();

        //Conflict with OBSTACLE?
        int   idxDir = GetDirectionIndex(inputDir);
        direction = GetTargetDirections(idxDir); //direction 배열이 명시적으로 보이지 않아서 이렇게 작성

        for (int i = 0; i < direction.Length; ++i)
        {
            int idxTarget = GetDirectionIndex(direction[i]);
            colPoint1 = CMath.Floor1000Vector3(position + cacheDirection[(idxTarget + 1)      % 16] * (delta + VOXEL_HALF_SIZE));
            colPoint2 = CMath.Floor1000Vector3(position + cacheDirection[(idxTarget - 1 + 16) % 16] * (delta + VOXEL_HALF_SIZE));

            float signed = sign;
            for (int j = 0; j < 3; ++j)
            {
                float y = signed * VOXEL_HALF_SIZE;
                if (!IsCollidedOrNull(map, colPoint1 + new Vector3(0f, y, 0f)) && !IsCollidedOrNull(map, colPoint2 + new Vector3(0f, y, 0f)))
                {
                    inputDir = direction[i];
                    goto MOVE;
                }
                signed += 1;
                if (signed > 1)
                { 
                    signed = -1; 
                }
            }
        }

        Debug.Log("Can`t move here. Move to oppsite side of last input?");
        return;

    MOVE:

        Vector3 dir = inputDir;
        dir.Normalize();

        //get y value
        int key = Parser.GetVoxelKeyFromPoint(position);
        if (map.TryGetValue(key, out Voxel_t voxel))
        {
            float dot = Vector3.Dot(inputDir, voxel.SlopeDirection);
            float radian;
            sign = 0f;
            if      (dot > 0) { sign =  1f; }
            else if (dot < 0) { sign = -1f; }

            if (voxel.SUB == 0b_11_11_11_11)
            { 
                radian = sign  * 45 * Mathf.Deg2Rad; //degree == 45;
            } 
            else
            {
                radian = 0; 
            }

            dir += new Vector3(0f, sign * Mathf.Sin(radian), 0f);
        }
        else
        {
            Debug.LogError($"Impossible voxel in position;");
            return;
        }

        transform.position += delta * dir;

        //Set Move Priority
        if      (inputDir.x > 0) { hasRightPriority = true;  }
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
    private Vector3[] GetTargetDirections(int index)
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

        return direction;
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawLine(colPoint1, colPoint2);
    }
}