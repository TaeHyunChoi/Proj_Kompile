using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMgr
{
    private static Camera mainCam;
    private static Camera battleCam;

    public static void Init(Transform tf)
    {
        mainCam     = tf.GetChild(0).GetComponent<Camera>();
        battleCam   = tf.GetChild(1).GetComponent<Camera>();

        OnBattleCam(false);
    }

    public static void OnBattleCam(bool on)
    {
        mainCam.enabled = !on;
        battleCam.enabled = on;
    }
}
