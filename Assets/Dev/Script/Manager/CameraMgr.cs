using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CameraMgr
{
    private static Camera mainCam;
    private static Camera battleCam;

    private static Vector3 mainOffset;

    public static void Init(Transform tf)
    {
        mainCam     = tf.GetChild(0).GetComponent<Camera>();
        battleCam   = tf.GetChild(1).GetComponent<Camera>();

        mainOffset = mainCam.transform.position;
        OnBattleCam(false);
    }

    public static void FollowPC(Vector3 pos)
    {
        mainCam.transform.position = pos + mainOffset;
    }

    public static void OnBattleCam(bool on)
    {
        mainCam.enabled = !on;
        battleCam.enabled = on;
    }
    public static Vector3 Battle_ScreenToLocalInRect(Vector3 targetPos)
    {
        Vector3 screenPoint = battleCam.WorldToScreenPoint(targetPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(UIMgr.Canvas_Battle.GetComponent<RectTransform>(), screenPoint, battleCam, out Vector2 localPoint);
        return localPoint;
    }
}
