using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIMgr : MonoBehaviour
{
    public static Canvas Canvas_Main { get; private set; }
    public static Canvas Canvas_Battle { get; private set; }



    private void Awake()
    {
        Canvas_Main      = transform.GetChild(0).GetComponent<Canvas>();
        Canvas_Battle    = transform.GetChild(1).GetComponent<Canvas>();
    }
    public static void Show(int type, bool on)
    {
        switch (type)
        {
            case IDxUI.BATTLE:   UIBattle.Show(on);      break;
        }
    }
}
