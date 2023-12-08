using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static x_UIBattle;
using static x_BIT;


public class x_UIBattle : MonoBehaviour
{
    public  static x_UIBattle Instance { get => instance; }
    private static x_UIBattle instance;

    private UIBattle_Menu       uiMenu;
    private UIBattle_Targeting  uiTarget;

    public static x_Unit NowUnit { get => x_UnitMgr.InBattle[x_GameMgr.NowOrder]; }

    private int select;    //[Targeting][Menu][Content]

    public static void Instantiate()
    {
        if (instance != null)
        {
            return;
        }

        GameObject obj = Resources.Load<GameObject>("Prefab/UIBattle");
        obj = Instantiate(obj, x_UIMgr.Canvas_Battle.transform);
        instance = obj.AddComponent<x_UIBattle>();
        instance.Init();
    }
    private void Init()
    {
        select = 0;

        uiMenu = new UIBattle_Menu(transform);
        uiTarget = new UIBattle_Targeting(transform);
    }

    public void Input(int state, int input)
    {
        switch (state)
        {
            case x_IDxSTATE.BATTLE_PLY_MENU:   select = uiMenu.ProcInput(select, input);   break;
            case x_IDxSTATE.BATTLE_PLY_TARGET: select = uiTarget.ProcInput(select, input); break;
        }
    }
    public void UI_SetInactive(int state)
    {
        //더 많아질 것 같으니 switch로 판 깔아둠
        switch (state)
        {
            case x_IDxSTATE.BATTLE_PLY_MENU:   uiMenu.  UI_SetActive(false, select, null);   break;
            case x_IDxSTATE.BATTLE_PLY_TARGET: uiTarget.UI_SetActive(false, select);         break;
        }
    }
    public void UIProc_SetActive(int state)
    {
        //[입력] 마지막 선택값
        select = NowUnit.LastSelect;

        //[처리] 게임 상태값
        x_GameMgr.State_Set(state);

        if (state == x_IDxSTATE.BATTLE_PLY_MENU)
        {
            //[처리] 메뉴 항목 개수 갱신 + 메뉴 정보 초기화
            select = uiMenu.SlotText_GetSlotInfo(select, out string[,] info);

            //[출력]
            uiMenu.UI_SetActive(true, select, info);
        }
        if (state == x_IDxSTATE.BATTLE_PLY_TARGET)
        {
            //[처리] 예외적으로 입력값 초기화 (ex. 스킬 최초 선택 => 타겟 자동 선택)
            if (((select & MASK_NOW_TARGET) >> SHIFT_TARGET) == 0)
            {
                select = uiTarget.ProcInput(select, 0);
                NowUnit.LastSelect_Update(select);
            }

            //[출력]
            uiTarget.UI_SetActive(true, select);
        }
    }
}
public class UIBattle_Menu
{
    public struct UISlot_Battle
    {
        private GameObject go;
        private Image icon;
        private TextMeshProUGUI name;

        public UISlot_Battle(GameObject _go)
        {
            go = _go;
            icon = _go.transform.GetChild(0).GetComponent<Image>();
            name = _go.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        }
        public void Load(string slotName, string rcsCode)
        {
            name.text = slotName;
            icon.sprite = x_ResourceMgr.SPIcon[rcsCode];
            go.SetActive(true);
        }
        public void SetActive(bool on)
        {
            go.SetActive(on);
        }
    }

    private GameObject obj;

    private RectTransform menuArrow;
    private RectTransform contentArrow;

    private TextMeshProUGUI[] contentTitleText;
    private GameObject prefabSlot;
    private Transform contentScroll;
    private List<UISlot_Battle> slots;

    private static Vector2 menuArrowDefault;
    private static Vector2 contentArrowDefault;
    private static float deltaMenu = 150f;
    private static float deltaContent = -125f;

    public UIBattle_Menu(Transform root)
    {
        prefabSlot = x_ResourceMgr.Prefab["UIBattle_MenuSlot"];
        obj = root.GetChild(0).gameObject;

        Transform menu = root.GetChild(0).GetChild(0);
        menuArrow = menu.GetChild(3).GetComponent<RectTransform>();
        menuArrowDefault = menuArrow.anchoredPosition;

        Transform content = root.GetChild(0).GetChild(1);
        contentArrow = content.GetChild(3).GetComponent<RectTransform>();
        contentArrowDefault = contentArrow.anchoredPosition;

        slots = new List<UISlot_Battle>();
        contentScroll = content.GetChild(2).GetChild(0).GetChild(0);
        for (int i = 0; i < contentScroll.childCount; ++i)
        {
            slots.Add(new UISlot_Battle(contentScroll.GetChild(i).gameObject));
        }
        contentTitleText = content.GetChild(1).GetComponentsInChildren<TextMeshProUGUI>();

        obj.SetActive(false);
    }


    public  int ProcInput(int select, int input)
    {
        //입력
        select = Input(select, input);

        //처리
        select = SlotText_GetSlotInfo(select, out string[,] info);

        //출력
        UI_UpdatePanel(select, info);
        UI_UpdateArrow(select);

        return select;
    }
    private int Input(int select, int input)
    {
        int idxMenu = (select & MASK_NOW_MENU) >> SHIFT_MENU;
        int idxLast = (select & MASK_CNT_CONTENT) >> SHIFT_CNT_CONTENT;

        //## Update Select
        switch (input & x_IDxINPUT.ACTION)
        {
            case x_IDxINPUT.ENTER:
                NowUnit.LastSelect_Update(select);

                switch (idxMenu)
                {
                    case x_IDxUI.BATTLE_MODE: Debug.Log("Input Mode");    break;
                    case x_IDxUI.BATTLE_ITEM: Debug.Log("Input Item");    break;
                    default:
                        Instance.UI_SetInactive(x_IDxSTATE.BATTLE_PLY_MENU);
                        Instance.UIProc_SetActive(x_IDxSTATE.BATTLE_PLY_TARGET);
                        return NowUnit.LastSelect;
                }

                return select;
            case x_IDxINPUT.CANCEL:
                return select;
        }
        switch (input & x_IDxINPUT.DIRECTION)
        {
            case x_IDxINPUT.RIGHT:
                //마지막 메뉴?
                if (idxMenu == (int)x_IDxUI.BATTLE_MAX - 1)
                {
                    select = 0; //MENU 초기화 → MENU =0, CONTENT 초기화 (TARGET만 남는다)
                    break;
                }

                select += (1 << SHIFT_MENU);
                select &= ~MASK_NOW_CONTENT;        //MENU 변경 → CONTENT 초기화
                break;
            case x_IDxINPUT.LEFT:

                //맨앞의 메뉴?
                if (idxMenu == 0)
                {
                    select = ((x_IDxUI.BATTLE_MAX - 1) << SHIFT_MENU); //CONTENT 초기화
                    break;
                }

                select -= (1 << SHIFT_MENU);
                select &= ~MASK_NOW_CONTENT;   //MENU 변경 → CONTENT 초기화
                break;
            case x_IDxINPUT.DOWN:
                if ((select & MASK_NOW_CONTENT) == idxLast)
                {
                    select &= ~MASK_NOW_CONTENT; //CONTENT 초기화                
                }
                else
                {
                    select += 0x01;
                }
                break;
            case x_IDxINPUT.UP:
                if ((select & MASK_NOW_CONTENT) == 0x00)
                {
                    select |= idxLast;
                }
                else
                {
                    select -= 0x01;
                }
                break;
        }

        return select;
    }


    public  void UI_SetActive(bool isOn, int select, string[,] info)
    {
        obj.SetActive(isOn);
        if (!isOn)
        {
            return;
        }

        //출력
        UI_UpdatePanel(select, info);
        UI_UpdateArrow(select);
    }
    private void UI_UpdatePanel(int select, string[,] info)
    {
        string[] text = new string[2];
        int menu = (select & MASK_NOW_MENU) >> SHIFT_MENU;
        int idxLast = (select & MASK_CNT_CONTENT) >> SHIFT_CNT_CONTENT;

        switch (menu)
        {
            case x_IDxUI.BATTLE_BASIC:
                text[0] = "기본기";
                text[1] = string.Empty;
                break;
            case x_IDxUI.BATTLE_SOLO:
                text[0] = "개인 공격기";
                text[1] = "MP";
                break;
            case x_IDxUI.BATTLE_GROUP:
                text[0] = "전체 공격기";
                text[1] = "MP";
                break;
            case x_IDxUI.BATTLE_MODE:
                text[0] = "모드";
                text[1] = string.Empty;
                break;
            case x_IDxUI.BATTLE_ITEM:
                text[0] = "아이템";
                text[1] = string.Empty;
                break;
            case x_IDxUI.BATTLE_SPECIAL:
                text[0] = "특수기";
                text[1] = string.Empty;
                break;
        }

        contentTitleText[0].text = text[0];
        contentTitleText[1].text = text[1];

        //Use Slot => New or Active(true)
        int i = 0;
        for (; i <= idxLast; ++i)
        {
            if (i >= slots.Count)
            {
                GameObject slot = GameObject.Instantiate(prefabSlot, contentScroll);
                slots.Add(new UISlot_Battle(slot));
            }

            slots[i].Load(info[0, i], info[1, i]);
        }
        //Not Used Slot => Active(false) : 앞선 i번째부터 이어서 체크하는구나
        for (; i < slots.Count; ++i)
        {
            slots[i].SetActive(false);
        }
    }
    private void UI_UpdateArrow(int select)
    {
        int menu = (select & MASK_NOW_MENU) >> SHIFT_MENU; ;
        int content = (select & MASK_NOW_CONTENT);

        menuArrow.anchoredPosition = menuArrowDefault + menu * new Vector2(deltaMenu, 0);
        contentArrow.anchoredPosition = contentArrowDefault + content * new Vector2(0, deltaContent);
    }


    public  int SlotText_GetSlotInfo(int select, out string[,] info)
    {
        info = new string[,] { };
        int menu = (select & MASK_NOW_MENU) >> SHIFT_MENU;
        int idxLast;

        switch (menu)
        {
            case x_IDxUI.BATTLE_BASIC:
                info = SlotText_GetSkillInfo(type: x_IDxSkill.BASIC, out idxLast);
                break;
            case x_IDxUI.BATTLE_SOLO:
                info = SlotText_GetSkillInfo(type: x_IDxSkill.SOLO, out idxLast);
                break;
            case x_IDxUI.BATTLE_GROUP:
                info = SlotText_GetSkillInfo(type: x_IDxSkill.GROUP, out idxLast);
                break;
            case x_IDxUI.BATTLE_MODE:
                info = SlotText_GetModeInfo(out idxLast);
                break;
            case x_IDxUI.BATTLE_ITEM:
                info = SlotText_GetItemInfo(out idxLast);
                break;
            case x_IDxUI.BATTLE_SPECIAL:
                info = SlotText_GetSkillInfo(type: x_IDxSkill.SPECIAL, out idxLast);
                break;
            default:
                idxLast = 1; //밑에서 (index - 1)= 0 으로 만들기 위함
                break;
        }

        select &= ~MASK_CNT_CONTENT;
        select |= ((idxLast - 1) << SHIFT_CNT_CONTENT);

        return select;
    }
    private string[,] SlotText_GetSkillInfo(int type, out int count)
    {
        SkillData[] skills = NowUnit.Skill[type];
        string[,] code = new string[2, skills.Length];
        count = code.GetLength(1);

        for (int i = 0; i < count; i++)
        {
            code[0, i] = skills[i].Name;
            code[1, i] = skills[i].RscCode;
        }

        return code;
    }
    private string[,] SlotText_GetModeInfo(out int count)
    {
        string[] mode = new string[] { "보통", "공격", "방어", "선제", "반격" };
        string[,] code = new string[2, mode.Length];
        count = code.GetLength(1);

        for (int i = 0; i < count; i++)
        {
            code[0, i] = mode[i];
            code[1, i] = "Icon_Mode"; //리소스 없음
        }

        return code;
    }
    private string[,] SlotText_GetItemInfo(out int count)
    {
        List<x_Player.Item> items = x_Player.Items;
        string[,] code = new string[2, items.Count];
        count = code.GetLength(1);

        for (int i = 0; i < count; i++)
        {
            code[0, i] = items[i].Tbl.Name;
            code[1, i] = items[i].Tbl.RcsCode;
        }

        return code;
    }
}
public class UIBattle_Targeting
{
    private GameObject obj;
    private RectTransform[] targetingArrows;

    public UIBattle_Targeting(Transform root)
    {
        obj = root.GetChild(1).gameObject;

        RectTransform[] temp = root.GetChild(1).GetComponentsInChildren<RectTransform>(true);
        targetingArrows = new RectTransform[temp.Length - 1];
        for (int i = 1; i < temp.Length; ++i)
        {
            targetingArrows[i - 1] = temp[i].GetComponent<RectTransform>();
            targetingArrows[i - 1].gameObject.SetActive(false);
        }

        obj.SetActive(false);
    }

    public int ProcInput(int select, int input)
    {
        select = Input(select, input);
        UI_UpdateArrow(select);

        return select;
    }
    private int Input(int select, int input)
    {
        //select는 [0b_0111_1111] 형태이다.
        int idxMenu = (select & MASK_NOW_MENU) >> SHIFT_MENU;
        int idxContent = select & MASK_NOW_CONTENT;
        int flagTarget = select >> SHIFT_TARGET;

        //input => Get Data
        SkillData skill = NowUnit.Skill[idxMenu][idxContent];
        int min = (skill.TargetGroupType == 1) ? 3 : 0;
        int max = (skill.TargetGroupType == 1) ? 6 : 2;

        min = 1 << min;
        max = 1 << max;

        //input & data => select_exceptional
        if (flagTarget == 0
            || skill.TargetGroupType == x_IDxSkill.TARGET_SELF
            || skill.TargetCountType == x_IDxSkill.TARGET_ALL)
        {
            //기본으로 세팅된 입력값(Self, All은 계산 방법이 항상 동일하다.)
            flagTarget = FlagTarget_SetDefaultIndex(flagTarget, skill.TargetGroupType, skill.TargetCountType, FlagTarget_GetIndex(min), FlagTarget_GetIndex(max));

            //입력처리를 스킵하지만 + UI Update는 가능하다.
            input = 0;
        }

        //input & data => select
        x_Unit target;
        switch (input & x_IDxINPUT.ACTION)
        {
            case x_IDxINPUT.ENTER:
                Instance.UI_SetInactive(x_IDxSTATE.BATTLE_PLY_TARGET);
                NowUnit.LastSelect_Update(select);
                NowUnit.BattleProc_Attack();
                return select;
            case x_IDxINPUT.CANCEL:
                Instance.UI_SetInactive(x_IDxSTATE.BATTLE_PLY_TARGET);
                Instance.UIProc_SetActive(x_IDxSTATE.BATTLE_PLY_MENU);
                return select;
        }
        switch (input & x_IDxINPUT.DIRECTION)
        {
            case x_IDxINPUT.UP:
                while (true)
                {
                    if (flagTarget == min)
                        flagTarget = max;
                    else
                        flagTarget >>= 1;

                    int idxPos = FlagTarget_GetIndex(flagTarget);
                    target = x_UnitMgr.InBattle[idxPos];
                    if (target != null && !target.IsFaint)
                        break;
                }
                break;
            case x_IDxINPUT.DOWN:
                while (true)
                {
                    if (flagTarget == max)
                        flagTarget = min;
                    else
                        flagTarget <<= 1;

                    int idxPos = FlagTarget_GetIndex(flagTarget);
                    target = x_UnitMgr.InBattle[idxPos];
                    if (target != null && !target.IsFaint)
                        break;
                }
                break;
        }

        select &= ~MASK_NOW_TARGET;
        select |= (flagTarget << SHIFT_TARGET);

        return select;
    }


    public void UI_SetActive(bool isOn, int select)
    {
        obj.SetActive(isOn);
        if (isOn)
        {
            UI_UpdateArrow(select);
        }
    }
    public void UI_UpdateArrow(int select)
    {
        select >>= SHIFT_TARGET;
        int comp;
        for (int i = 0; i < 7; ++i)
        {
            comp = select & (1 << i);
            comp >>= i;

            targetingArrows[i].gameObject.SetActive(comp != 0);
            if (comp == 0)
                continue;

            x_Unit unit = x_UnitMgr.InBattle[i];
            Vector3 pos = x_CameraMgr.Battle_ScreenToLocalInRect(unit.Pos + unit.transform.up); //나중에 unit height를 곱하던가...
            targetingArrows[i].localPosition = pos;
            targetingArrows[i].gameObject.SetActive(true);
        }
    }


    private int FlagTarget_GetIndex(int flag)
    {
        for (int i = 0; i < 7; ++i)
        {
            if ((flag >> i) == 1)
            {
                return i;
            }
        }

        return 0;
    }
    private int FlagTarget_SetDefaultIndex(int flag, int targetGroup, int targetCount, int min, int max)
    {
        if (targetCount == x_IDxSkill.TARGET_ALL)
        {
            switch (targetGroup)
            {
                case x_IDxSkill.TARGET_PARTY: return (0b_0000_0111); ;
                case x_IDxSkill.TARGET_ENEMY: return (0b_0111_1000); ;
                case x_IDxSkill.TARGET_XORSELF: return ~(1 << x_GameMgr.NowOrder); ;
            }
        }
        else if (targetCount == x_IDxSkill.TARGET_ONE)
        {
            switch (targetGroup)
            {
                case x_IDxSkill.TARGET_PARTY:
                case x_IDxSkill.TARGET_ENEMY:
                    {
                        x_Unit target;
                        for (int i = min; i < max; ++i)
                        {
                            target = x_UnitMgr.InBattle[i];
                            if (target != null && !target.IsFaint)
                            {
                                return (1 << i);
                            }
                        }
                    }
                    return flag;
                case x_IDxSkill.TARGET_SELF:
                    return (1 << x_GameMgr.NowOrder);
            }
        }

        return flag;
    }
}