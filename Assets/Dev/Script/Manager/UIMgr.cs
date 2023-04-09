using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIMgr
{
    public static Canvas Canvas_Main { get; private set; }
    public static Canvas Canvas_Battle { get; private set; }

    public static void Init(Transform tf)
    {
        Canvas_Main = tf.GetChild(0).GetComponent<Canvas>();
        Canvas_Battle = tf.GetChild(1).GetComponent<Canvas>();

        UIBattle.Init();
    }

    public static void Show(int type, bool on)
    {
        switch (type)
        {
            case IDxUI.BATTLE:   UIBattle.Show(on);      break;
        }
    }
}
