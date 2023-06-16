using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIMgr
{
    public static Canvas Canvas_Main { get; private set; }
    public static Canvas Canvas_Battle { get; private set; }

    private static Vector3 offset; //카메라 기울기로 인한 화면 상의 위치 각도 조절

    public static void Init(Transform tf)
    {
        Canvas_Main = tf.GetChild(0).GetComponent<Canvas>();
        Canvas_Battle = tf.GetChild(1).GetComponent<Canvas>();

        UIBattleSelect_2.Instantiate();
        UIBattleCombo.Init();

        offset = 0.35f * new Vector3(0, Mathf.Sin(Mathf.Deg2Rad * 50f), Mathf.Cos(Mathf.Deg2Rad * 50f));
    }
    public static void Show(int type, bool on)
    {
        switch (type)
        {
            //case IDxUI.BATTLE_SELECT:   UIBattleSelect.Show(on);      break;
            case IDxUI.BATTLE_SELECT: UIBattleSelect_2.Instance.Show(on); break;
            case IDxUI.BATTLE_COMBO: UIBattleCombo.Show(on); break;
        }
    }
    public static void BattleUI_InitTargetingArrows(Unit[] units)
    {
        UIBattleSelect_2.Instance.InitArrows(units, offset);
    }

    public static void Battle_SelectMenu(int input)
    {
        //UIBattleSelect.Select_Menu(input);
        UIBattleSelect_2.Instance.Input_Select(input);
    }
    public static void Battle_SelectTarget(int input)
    {
        UIBattleSelect_2.Instance.Input_Targeting(input);
    }

    //UI 클래스 만들어서 where T: 식으로 하는게 좋았으려나?
    public static bool UpdateUI_BattleCombo(bool active, float lerpWeight = 1)
    {
        return UIBattleCombo.Instance.UpdateUI(active, lerpWeight);
    }
}