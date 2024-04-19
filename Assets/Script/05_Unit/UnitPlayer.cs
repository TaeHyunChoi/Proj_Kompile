using System.Collections.Generic;
using UnityEngine;
using DataType;
using CMathf;

public class UnitPlayer : UnitBase
{
    private readonly float[] intervalRot  = new float[] { 0, 45f, -45f, 90f, -90f }; //시계방향 기준
    private readonly float SPEED_MOVE = 3f;

    private Vector3 dirBefore = new Vector3(-1f, 0, -1f);   // 직전의 이동 방향
    private float   scale = 1f;
    private int     layer = 0;
    public void Move(Dictionary<int, Tile_t> map, Vector3 dirInput)
    {
        Vector3 position = transform.position;

        //직전 이동 방향과 같은 방향이면 시계 방향으로, 그렇지 않다면 반시계 방향으로 탐색한다.
        float sign = Mathf.Sign(Vector3.Cross(dirInput, dirBefore).y) >= 0 ? 1f : -1f;
        for (int i = 0; i < intervalRot.Length; ++i)
        {
            //입력 방향을 회전시킨다.
            Vector3 dirRotated = Quaternion.Euler(0f, sign * intervalRot[i], 0f) * dirInput;
            dirRotated.Normalize();

            dirRotated *= Time.fixedDeltaTime * SPEED_MOVE * scale;
            Vector3 goal = CMath.FloorToVector(position + dirRotated, 3);

            int keyGoal = TileUtility.GetKey(layer, /*Vector3*/ goal, scale);
            keyGoal = TileUtility.GetKey_FromRelativeCoord(map, keyGoal, x: 0, z: 0);
            if (-1 == keyGoal)
            {
                continue;
            }

            //목표 지점에 타일이 존재하는가?
            if (false == map.TryGetValue(keyGoal, out Tile_t tileGoal))
            {
                return;
            }

            //감지 대상 삼각형을 배열에 저장한다.
            Vector3 pivot = TileUtility.GetPivot(goal, scale);
            int triangePoint = TileUtility.GetTriangleIndex(goal - pivot, scale * 0.5f);
            TileUtility.SetTriangleArray(map, triangePoint, keyGoal, pivot, scale);

            //UnitPlayer.Move() : 이동 가능할 때의 처리
            if (true == TileUtility.IsMovable(map, goal, scale))
            {
                //위치 변경
                float y = tileGoal.GetYValue(keyGoal, goal);
                goal = new Vector3(goal.x, y, goal.z);
                transform.position = goal;

                //트리거 호출
                if (true == tileGoal.HasTrigger(TileTrigger.Scale, out int flagScale))
                {
                    scale = (flagScale == 1) ? 0.5f : 1f;
                    Main.Cam.SetFOV(scale);
                    transform.localScale = Vector3.one * scale;
                }
                if (true == tileGoal.HasTrigger(TileTrigger.Layer, out int layer))
                {
                    this.layer = layer;
                    Main.Instance.SetFieldLayer(layer);
                }
                //필드 이벤트는 아직 미구현
                //if (true == tileMy.HasTrigger(TileTrigger.Event, out int code))
                //{
                //  //call event
                //}

                //직전의 입력 방향을 갱신
                float x = (0 != dirRotated.x) ? dirRotated.x : dirBefore.x;
                float z = (0 != dirRotated.z) ? dirRotated.z : dirBefore.z;
                dirBefore = new Vector3(x, y, z);

                return;
            }
        }
    }
}
