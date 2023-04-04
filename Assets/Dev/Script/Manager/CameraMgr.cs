using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMgr : MonoBehaviour
{
    public static Camera MainCam { get => cam; }
    private static Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }
    //카메라를 어찌 처리하면 좋으려나?
    void Update()
    {
        if (UnitMgr.MyPC == null
            || GameMgr.State == GameState.Battle)
            return;

        transform.position = UnitMgr.MyPC.Pos + new Vector3(0, 7f, -7f);
    }
}
