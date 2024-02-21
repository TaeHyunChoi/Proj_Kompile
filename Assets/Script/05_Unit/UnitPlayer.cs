using System.Collections.Generic;
using UnityEngine;
using static Public;
using CDataStructure;
using CMathf;
using static UnityEngine.RuleTile.TilingRuleOutput;
using UnityEngine.UIElements;

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

    private float lastSign;
    private bool hasRightPriority;
    private bool hasUpPriority;

    private Vector3 colPoint1, colPoint2;

    public void Move(Dictionary<int, Voxel_t> map, Vector3 inputDir)
    {
        Vector3 currentPos = CMath.Floor1000Vector3(transform.position);
        inputDir.Normalize();
        float delta = Time.deltaTime * MOVE_SPEED;
        float sign = lastSign; //-1, 0, 1 

        //Check navigation direction.
        direction = GetTargetDirections(inputDir);
        for (int i = 0; i < direction.Length; ++i)
        {
            int idxDir = GetDirectionIndex(direction[i]);
            colPoint1 = CMath.Floor1000Vector3(currentPos + cacheDirection[(idxDir + 1)      % 16] * (delta + VOXEL_HALF_SIZE));
            colPoint2 = CMath.Floor1000Vector3(currentPos + cacheDirection[(idxDir - 1 + 16) % 16] * (delta + VOXEL_HALF_SIZE));

            for (int j = 0; j < 3; ++j)
            {
                Vector3 offset = new Vector3(0f, sign * (VOXEL_HALF_SIZE + 0.001f), 0f);
                if (!IsCollidedOrNull(map, colPoint1 + offset) 
                    && !IsCollidedOrNull(map, colPoint2 + offset))
                {
                    inputDir = direction[i];
                    //Also get navigation direction(float sign).
                    goto SLOPE;
                }
                sign += 1;
                if (sign > 1)
                { 
                    sign = -1; 
                }
            }
        }

        Debug.Log("Can`t move here. Move to oppsite side of last input?");
        return;

    SLOPE:
        //try : 0
        Vector3 dir = new Vector3(inputDir.x, 0, inputDir.z);
        dir.Normalize();

        //Set the y value of the targeted coordinates.
        Vector3 targetPos = currentPos + delta * dir;
        Vector3 targetPivot = Parser.GetVoxelPivot(targetPos);
        int key = Parser.GetVoxelKeyFromPivot(targetPivot);
        if (map.TryGetValue(key, out Voxel_t targetVoxel))
        {
            //대상 복셀이 이동 가능한지 따진다? 여기서 또...?
            //흠...
            int sub = Parser.GetSubVoxelIndex(targetPivot, targetPos);
            sub = targetVoxel.GetSubType(sub);

            if (sub != OBSTACLE)
            {
                if (targetVoxel.SlopeFlag != 0)
                {
                    Vector3 lowPoint = targetPivot + (new Vector3(1f, 0f, 1f) - targetVoxel.SlopeDirection) * VOXEL_HALF_SIZE;
                    float size = Vector3.Distance(lowPoint, targetPos);
                    float angle = Vector3.Angle(from: targetVoxel.SlopeDirection, to: targetPos - lowPoint);
                    angle = (angle + 180) % 180; //unsigned

                    targetPos = new Vector3(targetPos.x, CMath.Floor1000(targetPivot.y + (Mathf.Cos(angle * Mathf.Deg2Rad) * size)), targetPos.z);
                }
                else
                {
                    targetPos = new Vector3(targetPos.x, targetPivot.y, targetPos.z);
                }

                goto MOVE;
            }
        }

        //try : sign
        dir = new Vector3(inputDir.x, sign, inputDir.z);
        dir.Normalize();
        targetPos = currentPos + delta * dir;
        targetPivot = Parser.GetVoxelPivot(targetPos);
        key = Parser.GetVoxelKeyFromPivot(targetPivot);
        if (map.TryGetValue(key, out targetVoxel))
        {
            int sub = Parser.GetSubVoxelIndex(targetPivot, targetPos);
            sub = targetVoxel.GetSubType(sub);

            if (sub != OBSTACLE)
            {
                if (targetVoxel.SlopeFlag != 0)
                {
                    Vector3 lowPoint = targetPivot + (new Vector3(1f, 0f, 1f) - targetVoxel.SlopeDirection) * VOXEL_HALF_SIZE;
                    float size = Vector3.Distance(lowPoint, targetPos);
                    float angle = Vector3.Angle(from: targetVoxel.SlopeDirection, to: targetPos - lowPoint);
                    angle = (angle + 180) % 180; //unsigned

                    targetPos = new Vector3(targetPos.x, CMath.Floor1000(targetPivot.y + (Mathf.Cos(angle * Mathf.Deg2Rad) * size)), targetPos.z);
                }
                else
                {
                    targetPos = new Vector3(targetPos.x, targetPivot.y, targetPos.z);
                }
            }
        }

    MOVE:
        transform.position = CMath.Floor1000Vector3(targetPos);
        lastSign = sign;

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

        Vector3 y = new Vector3(0f, VOXEL_HALF_SIZE - 0.001f, 0f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(colPoint1, colPoint1 - y);
        Gizmos.DrawLine(colPoint2, colPoint2 - y);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(colPoint1, colPoint1 + y);
        Gizmos.DrawLine(colPoint2, colPoint2 + y);
    }
}