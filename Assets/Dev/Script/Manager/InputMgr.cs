using UnityEngine;

public class InputMgr
{
    public delegate void InputDelegate();
    public static InputDelegate Update;

    private static InputKey input = InputKey.None;


    //Default
    public static void Set(InputMode type)
    {
        //기본 입력값
        Update = Base;

        //상황별 입력값 추가
        switch (type)
        {
            case InputMode.Field_Moving:       Update += Field;             break;
            case InputMode.Battle_Menu:      Update += BattleAction;      break;
            case InputMode.Battle_Targeting:   Update += BattleTargeting;   break;
            default: break;
        }

        //입력값 초기화
        Update += Reset;
    }
    private static void Base()
    {
        if (Input.GetButtonDown("Up"))
            input |= InputKey.Up;
        if (Input.GetButtonDown("Down"))
            input |= InputKey.Down;
        if (Input.GetButtonDown("Left"))
            input |= InputKey.Left;
        if (Input.GetButtonDown("Right"))
            input |= InputKey.Right;

        if (Input.GetButtonDown("Z"))
            input |= InputKey.Confirm;
        if (Input.GetButtonDown("X"))
            input |= InputKey.Cancel;
        if (Input.GetButtonDown("C"))
            input |= InputKey.Info;

        //테스트
        Test();
    }
    

    private static void Field()
    {
        if((input & InputKey.Direction) > 0)
            UnitMgr.PlayerMoveTo(input);

        if ((input & InputKey.Confirm) > 0)
            Debug.Log("Confirm");
    }
    private static void BattleAction()
    {
        if (Input.GetButton("C"))
            input |= InputKey.Info;

        if (input > 0)
            UIBattle.SelectMenu(input);
    }
    private static void BattleTargeting()
    {
        //TODO: InputKey."C"가 필요할까? >> UI에 편입시키는 것도 고려
        if (Input.GetButton("C"))
            input |= InputKey.Info;

        if (input > 0)
            UIBattle.SelectTarget(input);
    }


    private static void Test()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            GameMgr.EnterBattle();
    }
    
    
    private static void Reset()
    {
        input ^= input;
    }
}