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

        UIBattleSelect.Init();
        UIBattleCombo.Init();
    }

    public static void Show(int type, bool on)
    {
        switch (type)
        {
            case IDxUI.BATTLE_SELECT:   UIBattleSelect.Show(on);      break;
            case IDxUI.BATTLE_COMBO:    UIBattleCombo.Show(on);       break;
        }
    }


    public static void Battle_SelectMenu(int input)
    {
        UIBattleSelect.Select_Menu(input);
    }
    public static void Battle_SelectTarget(int input)
    {
        UIBattleSelect.Select_Target(input);
    }
    public static void Battle_SelectCombo(int input)
    { 
        
    }
}
