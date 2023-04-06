using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIMgr : MonoBehaviour
{
    public static Canvas UICanvas { get; private set; }

    private void Awake()
    {
        UICanvas = transform.GetChild(0).GetComponent<Canvas>();
    }
    public static void Show(int type, bool on)
    {
        switch (type)
        {
            case IDxUI.BATTLE:   UIBattle.Show(on);      break;
        }
    }
}
