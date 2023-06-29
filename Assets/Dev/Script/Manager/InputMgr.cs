using UnityEngine;
using static IDxINPUT;

public class InputMgr
{
    public delegate void InputDelegate();
    public static InputDelegate DUpdate;

    private static int  input = NONE;
    private static bool isComboPossible;

    public static void Update()
    {
        input = 0;

        //[입력1] System
        GetButtonDown_System();
        if (input != 0)
        {
            Debug.Log("Input System Key;");
            return;
        }

        //[입력2-1] 공통
        GetButtonDown_Direction();
        GetButtonDown_Action();

        //[입력2-2] 개별입력 => [처리] 상황별 호출
        switch (GameMgr.State)
        {
            case IDxSTATE.FIELD:
                GetButton_Direction();
                if (input != 0)
                {
                    UnitMgr.Field_PlayerMoveTo(input);
                }
                break;
            case IDxSTATE.BATTLE_PLY_MENU:
                if (input != 0)
                {
                    UIMgr.Battle_SelectMenu(input);
                }
                break;
            case IDxSTATE.BATTLE_PLY_TARGET:
                if (input != 0)
                {
                    UIMgr.Battle_SelectTarget(input);
                }
                break;
            case IDxSTATE.BATTLE_PLY_COMBO:
                if (input != 0)
                {
                    BattleCombo();
                }
                break;
        }
    }

    //## Common
    private static void GetButtonDown_Direction()
    {
        if (Input.GetButtonDown("Up"))
        {
            input |= UP;
        }
        if (Input.GetButtonDown("Down"))
        {
            input |= DOWN;
        }
        if (Input.GetButtonDown("Left"))
        {
            input |= LEFT;
        }
        if (Input.GetButtonDown("Right"))
        {
            input |= RIGHT;
        }
    }
    private static void GetButtonDown_Action()
    {
        if (Input.GetButtonDown("Enter"))
        {
            input |= ENTER;
        }
        if (Input.GetButtonDown("Cancel"))
        {
            input |= CANCEL;
        }
        if (Input.GetButtonDown("Trigger"))
        {
            input |= TRIGGER;
        }
    }
    private static void GetButtonDown_System()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            input |= CHEAT;
        }
        if (Input.GetButtonDown("Info"))
        {
            input |= INFO;
        }
        if (Input.GetButtonDown("Option"))
        {
            input |= OPTION;
        }
    }

    private static void GetButton_Direction()
    {
        if (Input.GetButton("Up"))
        {
            input |= UP;
        }
        if (Input.GetButton("Down"))
        {
            input |= DOWN;
        }
        if (Input.GetButton("Left"))
        {
            input |= LEFT;
        }
        if (Input.GetButton("Right"))
        {
            input |= RIGHT;
        }
    }

    public static bool IsInput()
    {
        //case by case
        switch (GameMgr.State)
        {
            case IDxSTATE.BATTLE_PLY_COMBO:
                if (Input.GetButtonUp("Trigger"))
                {
                    isComboPossible = false;
                }
                if (isComboPossible & Input.GetButtonDown("Trigger"))
                {
                    UIMgr.Show(IDxSTATE.BATTLE_PLY_COMBO, true);
                }
                if (isComboPossible & Input.GetButton("Trigger"))
                {
                    input |= TRIGGER;
                }
                break;
        }

        return input != 0;
    }

    private static void BattleCombo()
    {
        if (!isComboPossible)
        {
            UIMgr.UpdateUI_BattleCombo(active: false);
            UnitMgr.Anime_PlaySlow(slow: false);
            return;
        }

        if ((input & TRIGGER) != 0)
        {
            UIMgr.UpdateUI_BattleCombo(active: true);
            UnitMgr.Anime_PlaySlow(slow: true);
        }
    }
    public static void Set_IsCombo(bool isOn)
    {
        isComboPossible = isOn;
    }
}