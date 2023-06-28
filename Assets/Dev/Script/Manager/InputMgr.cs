using UnityEngine;
using static IDxINPUT;

public class InputMgr
{
    public delegate void InputDelegate();
    public static InputDelegate Update;

    private static int input = NONE;
    private static bool isComboPossible;

    public static void SetMode(int type)
    {
        //기본 입력값
        Update =  Base;
        Update += Option;

        //상황별 입력값 추가
        switch (type)
        {
            case FIELD:            Update += Field;             break;
            case BATTLE_MENU:      Update += BattleAction;      break;
            case BATTLE_TARGERT:   Update += BattleTargeting;   break;
            case BATTLE_COMBO:     Update += BattleCombo;       break;
            case CHEAT:            Update += Cheat;             break;
            default: break;
        }

        //입력값 초기화
        Update += delegate { input = 0; };
    }


    private static void Base()
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
        if (Input.GetButtonDown("Enter"))
        {
            input |= ENTER;
        }
        if (Input.GetButtonDown("Cancel"))
        {
            input |= CANCEL;
        }
        if (Input.GetButtonDown("Option"))
        {
            input |= OPTION;
        }
    }
    private static void Option()
    { 
        
    }


    private static void Field()
    {
        //GetButton: 꾸욱 눌러도 입력되도록
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
        if ((input & DIRECTION) != 0)
        {
            UnitMgr.Field_PlayerMoveTo(input);
        }

        //npc, 아이템 등과의 상호작용 용도
        if ((input & ENTER) != 0)
        {
            Debug.Log("ENTER");
        }
    }
    private static void BattleAction()
    {
        if (input != 0)
        {
            UIMgr.Battle_SelectMenu(input);
        }
    }
    private static void BattleTargeting()
    {
        if (input != 0)
        {
            UIMgr.Battle_SelectTarget(input);  //Update Input        
        }
    }
    private static void BattleCombo()
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

        if (isComboPossible & Input.GetButtonDown("Trigger"))
        {
            UIMgr.Show(IDxUI.BATTLE_COMBO, true);
        }
        if (isComboPossible & Input.GetButton("Trigger"))
        {
            input |= TRIGGER;
        }
        if (Input.GetButtonUp("Trigger"))
        {
            isComboPossible = false;
        }

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
        if ((input & DIRECTION) != 0)
        { 
            
        }
    }

    public static void Set_IsCombo(bool isOn)
    {
        isComboPossible = isOn;
    }

    private static void Cheat()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameMgr.Battle_Enter();
        }
    }
}