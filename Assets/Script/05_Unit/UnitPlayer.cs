using System.Collections.Generic;
using UnityEngine;
using DataType;
using CMathf;
using static PTile;

public class UnitPlayer : UnitBase
{
    private readonly float[] intervalRot = new float[] { 0, 1, -1, 2, -2 }; //clock-wise
    private readonly float SPEED_MOVE = 2f;
    private Vector3 dirBefore;
    private float scale = 1f;
    private byte layer = 0;

    public void Move(Dictionary<int, Tile_t> map, Vector3 dirInput)
    {
        Vector3 pointNow = CMath.FloorToVector(transform.position, 3);
        int keyMy = GetKey(layer, pointNow, scale);
        if (false == map.TryGetValue(keyMy, out Tile_t tileNow))
        {
            Debug.LogAssertion("Impossible position " + pointNow);
            return;
        }

        scale = tileNow.GetScale();
        Debug.Log($"{pointNow:F3}, key:{GetKey(layer, pointNow, scale)}, flag:{tileNow.Info >> 12} => scale:{scale}");

        int keyNow = PTile.GetKey(layer, pointNow, scale);
        float dist = CMath.Floor(Time.deltaTime * SPEED_MOVE, 3);
        float rotY = Mathf.Sign(Vector3.Cross(dirInput, dirBefore).y);
        int sign = (rotY >= 0) ? 1 : -1;

        for (int d = 0; d < 5; ++d)
        {
            Vector3 dirRotated = Quaternion.Euler(0f, sign * intervalRot[d] * 45f, 0f) * dirInput;
            dirRotated.Normalize();
            dirRotated = CMath.FloorToVector(dirRotated * (SIZE_QUATER + dist) * scale, 3);

            //collide?
            for (int c = 0; c < 3; ++c)
            {
                Vector3 dirCollide = Quaternion.Euler(0f, intervalRot[c] * 45f, 0f) * dirRotated;
                dirCollide.Normalize();
                dirCollide = CMath.FloorToVector(dirCollide * (SIZE_QUATER + dist) * scale, 3);

                Vector3 pointCollide = CMath.FloorToVector(pointNow + dirCollide, 3);
                if (false == CanMoveTo(map, pointCollide, keyNow, tileNow))
                {
                    goto CONTINUE;
                }
            }

            dirInput = dirRotated;
            goto SET_POSITION;

        CONTINUE:
            continue;
        }

        Debug.Log("Can`t Move...");
        //TODO: Need to dev: Can`t move, diagonal direction;
        return;

    SET_POSITION:
        dirInput.Normalize();
        dirInput = CMath.FloorToVector(dirInput * dist, 3);

        Vector3 pointGoal = CMath.FloorToVector(pointNow + dirInput * scale, 3);
        int keyGoal = PTile.GetKey(layer, pointGoal, scale);

        for (sign = 1; sign >= -1; --sign)
        {
            int key = keyGoal + sign * (1 << 8);
            if (true == map.TryGetValue(key, out Tile_t tileGoal))
            {
                float y = tileGoal.GetYValue(key, pointGoal); //3차원이니까 제곱으로?
                transform.position = CMath.FloorToVector(new Vector3(pointGoal.x, y, pointGoal.z), 3);
                dirBefore = dirInput;

                if (0 != (tileGoal.Info >> 21))
                {
                    //여기서 트리거 발동시켜야 한다.
                    layer = 1;
                    scale = 0.5f;
                    transform.localScale = Vector3.one * scale;
                }
                return;
            }
        }
    }

    private bool CanMoveTo(Dictionary<int, Tile_t> map, Vector3 point, int keyMy, Tile_t tileMy)
    {
        //최소, 최대 범위 안에 있는가
        if (false == PTile.IsInGrid(point.x, point.z))
        {
            return false;
        }

        int keyTarget = PTile.GetKey(layer, point, tileMy.GetScale());
        for (int sign = -1; sign <= 1; ++sign)
        {
            int key = keyTarget + sign * (1 << 8);

            //유효한 타일?
            if (false == map.ContainsKey(key))
            {
                continue;
            }

            //같은 타일 + 해당 분면으로 이동 가능?
            if (key == keyMy)
            {
                if (false == tileMy.IsMovable(keyMy, point))
                {
                    continue;
                }
            }
            //or 다른 타일로 이동 가능?
            else if (false == tileMy.IsLinked(keyMy, point))
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
