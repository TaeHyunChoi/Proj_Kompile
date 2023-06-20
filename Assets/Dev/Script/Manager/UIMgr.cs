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
        //UIBattleCombo.Init(); //이걸 합쳐야 하는구나

        offset = 0.35f * new Vector3(0, Mathf.Sin(Mathf.Deg2Rad * 50f), Mathf.Cos(Mathf.Deg2Rad * 50f));
    }
    public static void Show(int type, bool on)
    {
        switch (type)
        {
            case IDxUI.BATTLE_MENU: 
            case IDxUI.BATTLE_TARGET: 
            case IDxUI.BATTLE_COMBO:
                UIBattle.Instance.Show(type, true); 
                break;
        }
    }
    public static void BattleUI_InitTargetingArrows(Unit[] units)
    {
        UIBattle.Instance.UpdateUI_Target(units, offset);
    }

    public static void Battle_SelectMenu(int input)
    {
        UIBattle.Instance.Input(type: 0, input);
    }
    public static void Battle_SelectTarget(int input)
    {
        UIBattle.Instance.Input(type: 1, input);
    }

    //UI 클래스 만들어서 where T: 식으로 하는게 좋았으려나?
    public static bool UpdateUI_BattleCombo(bool active, float lerpWeight = 1)
    {
        return UIBattleCombo.Instance.UpdateUI(active, lerpWeight);
    }
}