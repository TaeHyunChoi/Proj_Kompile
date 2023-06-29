using UnityEngine;

public class UIMgr
{
    public static Canvas Canvas_Main { get; private set; }
    public static Canvas Canvas_Battle { get; private set; }

    private static Vector3 offset; //카메라 기울기로 인한 화면 상의 위치 각도 조절

    public static void Init(Transform tf)
    {
        Canvas_Main = tf.GetChild(0).GetComponent<Canvas>();
        Canvas_Battle = tf.GetChild(1).GetComponent<Canvas>();

        UIBattle.Instantiate();
        /*다른 Layer도 쭉쭉 생성하면 된다?*/
    }
    public static void Show(int type, bool on)
    {
        switch (type)
        {
            case IDxSTATE.BATTLE_PLY_MENU: 
            case IDxSTATE.BATTLE_PLY_TARGET: 
            case IDxSTATE.BATTLE_PLY_COMBO:
                UIBattle.Instance.Active(type, on);
                break;
        }
    }
    public static void Battle_SelectMenu(int input)
    {
        UIBattle.Instance.Input(IDxSTATE.BATTLE_PLY_MENU, input);
    }
    public static void Battle_SelectTarget(int input)
    {
        UIBattle.Instance.Input(IDxSTATE.BATTLE_PLY_TARGET, input);
    }

    //UI 클래스 만들어서 where T: 식으로 하는게 좋았으려나?
    public static bool UpdateUI_BattleCombo(bool active, float lerpWeight = 1)
    {
        //return UIBattleCombo.Instance.UpdateUI(active, lerpWeight);
        return true;
    }
}