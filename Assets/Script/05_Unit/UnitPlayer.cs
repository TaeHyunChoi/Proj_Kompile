using System.Collections.Generic;
using UnityEngine;
using DataType;
using CMathf;
using static Index.IDxTile;

public class UnitPlayer : UnitBase
{
    private readonly float[] intervalRot  = new float[] { 0, 45f, -45f, 90f, -90f }; //clock-wise
    private readonly float SPEED_MOVE = 3f;

    private Vector3 dirBefore = new Vector3(-1f, 0, -1f);
    private float   scale = 1f;
    private int     layer = 0;

    public void Move(Dictionary<int, Tile_t> map, Vector3 dirInput)
    {
        float sign = Mathf.Sign(Vector3.Cross(dirInput, dirBefore).y) >= 0 ? 1f : -1f;

        for (int i = 0; i < intervalRot.Length; ++i)
        {
            Vector3 dirRotated = Quaternion.Euler(0f, sign * intervalRot[i], 0f) * dirInput;
            dirRotated.Normalize();

            dirRotated *= Time.fixedDeltaTime * SPEED_MOVE;
            Vector3 goal = transform.position + dirRotated;

            int keyMy = TileUtility.GetKey(layer, goal, scale);
            keyMy = TileUtility.GetKey_FromRelativeCoord(map, keyMy, 0, 0);
            if (-1 == keyMy)
            {
                //목적 지점에서 tile_t 정보를 찾을 수 없다면 return false;
                continue;
            }

            Vector3 pivot = TileUtility.GetPivot(goal, scale);
            int triangePoint = TileUtility.GetTriangleIndex(goal - pivot, scale * 0.5f);

            TriangleUtility.SetTriangleArray(map, triangePoint, keyMy, pivot, scale);

            if (TriangleUtility.IsMovable(goal, scale)
                && true == map.TryGetValue(keyMy, out Tile_t tileMy))
            {
                float y = tileMy.GetYValue(keyMy, goal);
                transform.position = CMath.FloorToVector(new Vector3(goal.x, y, goal.z), 3);

                float x = (0 != dirRotated.x) ? dirRotated.x : dirBefore.x;
                float z = (0 != dirRotated.z) ? dirRotated.z : dirBefore.z;
                dirBefore = new Vector3(x, y, z);
                return;
            }
        }
    }
}
