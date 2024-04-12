using System.Collections.Generic;
using UnityEngine;
using DataType;
using CMathf;
using static PTile;
using static Index.IDxTile;

public class UnitPlayer : UnitBase
{
    private readonly float[] intervalRot  = new float[] { 0, 45f, -45f, 90f, -90f }; //clock-wise
    private readonly float[] collisionRot = new float[] { 0, 15f, -15f, 30f, -30f, 45f, -45f, 60f, -60f, 75f, -75f };
    private readonly float SPEED_MOVE = 3f;

    private Vector3 dirBefore = new Vector3(-1f, 0, -1f);
    private float   scale = 1f;
    private int     layer = 0;

    public void Move(Dictionary<int, Tile_t> map, Vector3 dirInput)
    {
        Vector3 pointNow = CMath.FloorToVector(transform.position, 3);
        int keyMy = GetKey(layer, pointNow, scale);

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
        if (false == map.TryGetValue(keyMy, out Tile_t tileNow))
        {
            Debug.LogAssertion("Impossible position " + pointNow);
            return;
        }
#endif

        int keyNow = PTile.GetKey(layer, pointNow, scale);
        float dist = CMath.Floor(Time.deltaTime * SPEED_MOVE, 3);
        float rotY = Mathf.Sign(Vector3.Cross(dirInput, dirBefore).y);
        int sign = (rotY >= 0) ? 1 : -1;

        for (int d = 0; d < 5; ++d)
        {
            Vector3 dirRotated = Quaternion.Euler(0f, sign * intervalRot[d], 0f) * dirInput;
            //dirRotated.Normalize();
            //dirRotated = CMath.FloorToVector(dirRotated * (SIZE_QUATER + dist) * scale, 3);

            //collide?
            for (int c = 0; c < collisionRot.Length; ++c)
            {
                Vector3 dirCollision = Quaternion.Euler(0f, collisionRot[c], 0f) * dirRotated;
                dirCollision.Normalize();
                dirCollision = CMath.FloorToVector(dirCollision * (SIZE_QUATER + dist) * scale, 3);

                Vector3 pointCollision = CMath.FloorToVector(pointNow + dirCollision, 3);
                if (false == CanMoveTo(map, pointCollision, keyNow, tileNow))
                {
                    goto CONTINUE;
                }
            }

            dirInput = dirRotated;
            goto SET_POSITION;

        CONTINUE:
            continue;
        }

        //Stop without looking in another direction.
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
                pointGoal = new Vector3(pointGoal.x, y, pointGoal.z);
                transform.position = CMath.FloorToVector(pointGoal, 3);

                float x = (0 != dirInput.x) ? dirInput.x : dirBefore.x;
                float z = (0 != dirInput.z) ? dirInput.z : dirBefore.z;
                dirBefore = new Vector3(x, y, z);

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
