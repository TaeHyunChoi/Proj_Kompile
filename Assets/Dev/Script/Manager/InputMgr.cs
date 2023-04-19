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
        Update =  Base;
        Update += Option;

        //상황별 입력값 추가
        switch (type)
        {
            case IDxINPUT.MODE_FIELD:            Update += Field;             break;
            case IDxINPUT.MODE_BATTLE_MENU:      Update += BattleAction;      break;
            case IDxINPUT.MODE_BATTLE_TARGERT:   Update += BattleTargeting;   break;
            case IDxINPUT.MODE_CHEAT:            Update += Cheat;             break;
            default: break;
        }

        //입력값 초기화
        Update += Reset;
    }

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

        if (Input.GetButtonDown("Enter"))
            input |= IDxINPUT.ENTER;
        if (Input.GetButtonDown("Cancel"))
            input |= IDxINPUT.CANCEL;
        if (Input.GetButtonDown("Info"))
            input |= IDxINPUT.INFO;
    }
    private static void Option()
    { 
        
    }
    

    private static void Field()
    {
        //꾸욱 눌러도 돌아다닐 수 있도록
        if (Input.GetButton("Up"))
            input |= IDxINPUT.UP;
        if (Input.GetButton("Down"))
            input |= IDxINPUT.DOWN;
        if (Input.GetButton("Left"))
            input |= IDxINPUT.LEFT;
        if (Input.GetButton("Right"))
            input |= IDxINPUT.RIGHT;

        if ((input & IDxINPUT.DIRECTION) > 0)
            UnitMgr.Field_PlayerMoveTo(input);

        if ((input & IDxINPUT.ENTER) > 0)
            Debug.Log("ENTER");
    }
    private static void BattleAction()
    {
        if (Input.GetButton("Info"))
            input |= IDxINPUT.INFO;

        if (input > 0)
            UIBattle.Select_Menu(input);
    }
    private static void BattleTargeting()
    {
        //TODO: InputKey."C"가 필요할까? >> UI에 편입시키는 것도 고려
        if (Input.GetButton("Info"))
            input |= IDxINPUT.INFO;

        if (input > 0)
            UIBattle.Select_Target(input);
    }


    private static void Cheat()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            GameMgr.Battle_Enter();
    }
    
    
    private static void Reset()
    {
        input ^= input;
    }
}