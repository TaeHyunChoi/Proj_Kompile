using UnityEngine;

public class InputMgr
{
    public delegate void InputDelegate();
    public static InputDelegate Update;

    private static int input = IDxINPUT.NONE;


    //Default
    public static void Set(int type)
    {
        //기본 입력값
        Update = Base;

        //상황별 입력값 추가
        switch (type)
        {
            case IDxINPUT.FIELD:            Update += Field;             break;
            case IDxINPUT.BATTLE_MENU:      Update += BattleAction;      break;
            case IDxINPUT.BATTLE_TARGERT:   Update += BattleTargeting;   break;
            default: break;
        }

        //입력값 초기화
        Update += Reset;
    }

    //이참에 버튼명도 바꾸는 게 좋으려나...
    private static void Base()
    {
        if (Input.GetButtonDown("Up"))
            input |= IDxINPUT.UP;
        if (Input.GetButtonDown("Down"))
            input |= IDxINPUT.DOWN;
        if (Input.GetButtonDown("Left"))
            input |= IDxINPUT.LEFT;
        if (Input.GetButtonDown("Right"))
            input |= IDxINPUT.RIGHT;

        if (Input.GetButtonDown("Z"))
            input |= IDxINPUT.ENTER;
        if (Input.GetButtonDown("X"))
            input |= IDxINPUT.CANCEL;
        if (Input.GetButtonDown("C"))
            input |= IDxINPUT.INFO;

        //테스트
        Test();
    }
    

    private static void Field()
    {
        if((input & IDxINPUT.DIRECTION) > 0)
            UnitMgr.PlayerMoveTo(input);

        if ((input & IDxINPUT.ENTER) > 0)
            Debug.Log("ENTER");
    }
    private static void BattleAction()
    {
        if (Input.GetButton("C"))
            input |= IDxINPUT.INFO;

        if (input > 0)
            UIBattle.SelectMenu(input);
    }
    private static void BattleTargeting()
    {
        //TODO: InputKey."C"가 필요할까? >> UI에 편입시키는 것도 고려
        if (Input.GetButton("C"))
            input |= IDxINPUT.INFO;

        if (input > 0)
            UIBattle.SelectTarget(input);
    }


    private static void Test()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            GameMgr.Battle_Enter();
    }
    
    
    private static void Reset()
    {
        input ^= input;
    }
}