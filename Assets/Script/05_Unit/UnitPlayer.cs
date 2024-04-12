using System.Collections.Generic;
using UnityEngine;
using DataType;
using CMathf;
using static PTile;
using static Index.IDxTile;

public class UnitPlayer : UnitBase
{
    private readonly float[] intervalRot = new float[] { 0, 1, -1, 2, -2 }; //clock-wise
    private readonly float SPEED_MOVE = 4f;
    private Vector3 dirBefore;
    private float scale = 1f;
    private int layer = 0;

    public void Move(Dictionary<int, Tile_t> map, Vector3 dirInput)
    {
        Vector3 pointNow = CMath.FloorToVector(transform.position, 3);
        int keyMy = GetKey(layer, pointNow, scale);
        if (false == map.TryGetValue(keyMy, out Tile_t tileNow))
        {
            Debug.LogAssertion("Impossible position " + pointNow);
            return;
        }

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
                float y = tileGoal.GetYValue(key, pointGoal);
                transform.position = CMath.FloorToVector(new Vector3(pointGoal.x, y, pointGoal.z), 3);
                dirBefore = dirInput;

                if (false == tileGoal.IsTriggerNone())
                {
                    if (true == tileGoal.HasTrigger(TileTrigger.ScaleDown, out int isScaleDown))
                    {
                        scale = (0 != isScaleDown) ? 0.5f : 1f;
                        transform.localScale = Vector3.one * scale;
                        Main.Cam.SetFOV(scale);
                    }
                    if (true == tileGoal.HasTrigger(TileTrigger.Layer, out int indexLayer))
                    {
                        layer = indexLayer;
                        Main.Instance.GetContent<OnField>().SetLayer(layer);
                    }
                    if (true == tileGoal.HasTrigger(TileTrigger.Interact, out int codeInteract))
                    {
                        Debug.Log("Need to dev: interact trigger");
                    }
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

        int keyTarget = PTile.GetKey(layer, point, scale);
        Vector3 pivot = PTile.GetPivot(keyTarget, scale);

        for (int sign = -1; sign <= 1; ++sign)
        {
            int key = keyTarget + sign * (1 << SHIFT_KEY_Y);

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
