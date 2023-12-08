using UnityEngine;

public class x_UIMgr
{
    public static Canvas Canvas_Main { get; private set; }
    public static Canvas Canvas_Battle { get; private set; }

    //UIBattle.Instance로 받지 말고 계속 메모리로 들고 있으면 안되나?
    //'UI는 모두 자주 사용한다'라고 가정한다면? 흐으으음

    public static void Init(Transform tf)
    {
        Canvas_Main = tf.GetChild(0).GetComponent<Canvas>();
        Canvas_Battle = tf.GetChild(1).GetComponent<Canvas>();

        x_UIBattle.Instantiate();
        /*다른 Layer도 쭉쭉 생성하면 된다?*/
    }
    public static void Show(int type)
    {
        switch (type)
        {
            case x_IDxSTATE.BATTLE_PLY_MENU: 
            case x_IDxSTATE.BATTLE_PLY_TARGET: 
            case x_IDxSTATE.BATTLE_PLY_COMBO:
                x_UIBattle.Instance.UIProc_SetActive(type);
                break;
        }
    }
    
    //얘도 전파하는 식으로 가능할 듯?
    public static void Battle_Input(int state, int input)
    {
        x_UIBattle.Instance.Input(state, input);
    }
    public static bool Battle_UpdateUICombo(bool active, float lerpWeight = 1)
    {
        //return UIBattleCombo.Instance.UpdateUI(active, lerpWeight);
        return true;
    }
}