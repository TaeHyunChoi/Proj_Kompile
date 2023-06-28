using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static UIBattle;
using static BIT;

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
        icon.sprite = ResourceMgr.SPIcon[rcsCode];
        go.SetActive(true);
    }
    public void SetActive(bool on)
    {
        go.SetActive(on);
    }
}
public class UIBattle : MonoBehaviour
{
    public static UIBattle Instance { get => instance; }
    private static UIBattle instance;

    private UIBattle_Menu uiMenu;
    private UIBattle_Targeting uiTarget;
    private UIBattle_Combo uiCombo;

    public static Unit NowUnit { get => UnitMgr.InBattle[GameMgr.NowOrder]; }

    public static int BattleSelect { get => instance.select; }
    private int select; //[Targeting][Menu][Content]

    public static void Instantiate()
    {
        if (instance != null)
        {
            return;
        }

        GameObject obj = Resources.Load<GameObject>("Prefab/UIBattle");
        obj = Instantiate(obj, UIMgr.Canvas_Battle.transform);
        instance = obj.AddComponent<UIBattle>();
        instance.Init();
    }
    private void Init()
    {
        select = 0;

        uiMenu = new UIBattle_Menu(transform);
        uiTarget = new UIBattle_Targeting(transform);
        uiCombo = new UIBattle_Combo(transform);
    }

    public void Active(int type, bool isOn)
    {
        //근데 이 친구도 좀 더 효율적으로 만들 순 없나?
        //솔직히 퉁 치려는거 티나긴 함 ㅎㅎ!

        uiMenu.  Show(type == IDxUI.BATTLE_MENU   & isOn, NowUnit.LastSelect);
        uiTarget.Show(type == IDxUI.BATTLE_TARGET & isOn);
    }
    public void Input(byte type, int input)
    {
        switch (type)
        {
            case 0: select = uiMenu.ProcInput(select, input);   break;
            case 1: select = uiTarget.ProcInput(select, input); break;
        }
    }
}
public class UIBattle_Menu
{
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
        prefabSlot = ResourceMgr.Prefab["UIBattle_MenuSlot"];
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
            slots.Add(new UISlot_Battle(contentScroll.GetChild(i).gameObject));

        contentTitleText = content.GetChild(1).GetComponentsInChildren<TextMeshProUGUI>();

        Show(false, 0);
    }

    public int ProcInput(int select, int input)
    {
        //입력
        select = Input(select, input);

        //처리
        select = GetSlotContent(select, out string[,] info);
        InputMgr.SetMode(IDxINPUT.BATTLE_MENU);

        //출력
        UI_UpdatePanel(select, info);
        UI_UpdateArrow(select);

        return select;
    }
    private int GetSlotContent(int select, out string[,] info)
    {
        info = new string[,] { };
        int menu = (select & MASK_NOW_MENU) >> SHIFT_MENU;
        int idxLast;

        switch ((EIdxMENU)menu)
        {
            case EIdxMENU.SkillBasic:
                info = SlotText_GetSkillInfo(type: IDxSkill.BASIC, out idxLast);
                break;
            case EIdxMENU.SkillSolo:
                info = SlotText_GetSkillInfo(type: IDxSkill.SOLO, out idxLast);
                break;
            case EIdxMENU.SkillGroup:
                info = SlotText_GetSkillInfo(type: IDxSkill.GROUP, out idxLast);
                break;
            case EIdxMENU.Mode:
                info = SlotText_GetModeInfo(out idxLast);
                break;
            case EIdxMENU.Item:
                info = SlotText_GetItemInfo(out idxLast);
                break;
            case EIdxMENU.SkillSpecial:
                info = SlotText_GetSkillInfo(type: IDxSkill.SPECIAL, out idxLast);
                break;
            default:
                idxLast = 1; //밑에서 (index - 1)= 0 으로 만들기 위함
                break;
        }

        select &= ~MASK_CNT_CONTENT;
        select |= ((idxLast - 1) << SHIFT_CNT_CONTENT);

        return select;
    }
    private int Input(int select, int input)
    {
        int idxMenu = (select & MASK_NOW_MENU) >> SHIFT_MENU;
        int idxLast = (select & MASK_CNT_CONTENT) >> SHIFT_CNT_CONTENT;

        //## Update Select
        switch (input & IDxINPUT.INTERACT)
        {
            case IDxINPUT.ENTER:
                switch ((EIdxMENU)idxMenu)
                {
                    case EIdxMENU.Mode: ProcChangeMode(); break;
                    case EIdxMENU.Item: ProcUseItem(); break;
                    default:
                        Instance.Active(type: 1, true);
                        return BattleSelect;
                }
                return select;
            case IDxINPUT.CANCEL:
                return select;
            case IDxINPUT.OPTION:
                return select;
        }
        switch (input & IDxINPUT.DIRECTION)
        {
            case IDxINPUT.RIGHT:
                //마지막 메뉴?
                if (idxMenu == (int)EIdxMENU.Count - 1)
                {
                    select = 0; //MENU 초기화 → MENU =0, CONTENT 초기화 (TARGET만 남는다)
                    break;
                }

                select += (1 << SHIFT_MENU);
                select &= ~MASK_NOW_CONTENT;        //MENU 변경 → CONTENT 초기화
                break;
            case IDxINPUT.LEFT:

                //맨앞의 메뉴?
                if (idxMenu == 0)
                {
                    select = (((int)EIdxMENU.Count - 1) << SHIFT_MENU); //CONTENT 초기화
                    break;
                }

                select -= (1 << SHIFT_MENU);
                select &= ~MASK_NOW_CONTENT;   //MENU 변경 → CONTENT 초기화
                break;
            case IDxINPUT.DOWN:
                if ((select & MASK_NOW_CONTENT) == idxLast)
                {
                    select &= ~MASK_NOW_CONTENT; //CONTENT 초기화                
                }
                else
                {
                    select += 0x01;
                }
                break;
            case IDxINPUT.UP:
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
    private void UI_UpdatePanel(int select, string[,] info)
    {
        string[] text = new string[2];
        int menu = (select & MASK_NOW_MENU) >> SHIFT_MENU;
        int idxLast = (select & MASK_CNT_CONTENT) >> SHIFT_CNT_CONTENT;

        switch ((EIdxMENU)menu)
        {
            case EIdxMENU.SkillBasic:
                text[0] = "기본기";
                text[1] = string.Empty;
                break;
            case EIdxMENU.SkillSolo:
                text[0] = "개인 공격기";
                text[1] = "MP";
                break;
            case EIdxMENU.SkillGroup:
                text[0] = "전체 공격기";
                text[1] = "MP";
                break;
            case EIdxMENU.Mode:
                text[0] = "모드";
                text[1] = string.Empty;
                break;
            case EIdxMENU.Item:
                text[0] = "아이템";
                text[1] = string.Empty;
                break;
            case EIdxMENU.SkillSpecial:
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
        List<Player.Item> items = Player.Items;
        string[,] code = new string[2, items.Count];
        count = code.GetLength(1);

        for (int i = 0; i < count; i++)
        {
            code[0, i] = items[i].Tbl.Name;
            code[1, i] = items[i].Tbl.RcsCode;
        }

        return code;
    }

    private void ProcChangeMode()
    {

    }
    private void ProcUseItem()
    {

    }

    public void Show(bool isOn, int select)
    {
        obj.SetActive(isOn);
        if (!isOn)
        {
            return;
        }

        //입력(select) => 처리
        select = GetSlotContent(select, out string[,] info);
        InputMgr.SetMode(IDxINPUT.BATTLE_MENU);

        //출력
        UI_UpdatePanel(select, info);
        UI_UpdateArrow(select);
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

        Show(false);
    }

    public int ProcInput(int select, int input)
    {
        select = Input(select, input);

        InputMgr.SetMode(IDxINPUT.BATTLE_TARGERT);

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
            || skill.TargetGroupType == (int)ETargetGroup.Self
            || skill.TargetCountType == (int)ETargetCount.All)
        {
            //기본으로 세팅된 입력값(Self, All은 계산 방법이 항상 동일하다.)
            flagTarget = FlagTarget_SetDefaultPositionIndex(flagTarget, skill.TargetGroupType, skill.TargetCountType, FlagTarget_GetPositionIndex(min), FlagTarget_GetPositionIndex(max));

            //입력처리를 스킵하지만 + UI Update는 가능하다.
            input = 0;
        }

        //input & data => select
        Unit target;
        switch (input & IDxINPUT.INTERACT)
        {
            case IDxINPUT.ENTER:
                NowUnit.LastSelect_Update(select);
                NowUnit.ProcBattle_Attack();
                return select;
            case IDxINPUT.CANCEL:
                Instance.Active(type: 0, true);
                return select;
            case IDxINPUT.OPTION:
                return select;
        }
        switch (input & IDxINPUT.DIRECTION)
        {
            case IDxINPUT.UP:
                while (true)
                {
                    if (flagTarget == min)
                        flagTarget = max;
                    else
                        flagTarget >>= 1;

                    int idxPos = FlagTarget_GetPositionIndex(flagTarget);
                    target = UnitMgr.InBattle[idxPos];
                    if (target != null && !target.IsFaint)
                        break;
                }
                break;
            case IDxINPUT.DOWN:
                while (true)
                {
                    if (flagTarget == max)
                        flagTarget = min;
                    else
                        flagTarget <<= 1;

                    int idxPos = FlagTarget_GetPositionIndex(flagTarget);
                    target = UnitMgr.InBattle[idxPos];
                    if (target != null && !target.IsFaint)
                        break;
                }
                break;
        }

        select &= ~MASK_NOW_TARGET;
        select |= (flagTarget << SHIFT_TARGET);

        return select;
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

            Unit unit = UnitMgr.InBattle[i];
            Vector3 pos = CameraMgr.Battle_ScreenToLocalInRect(unit.Pos + unit.transform.up); //나중에 unit height를 곱하던가...
            targetingArrows[i].localPosition = pos;
            targetingArrows[i].gameObject.SetActive(true);
        }
    }

    private int FlagTarget_GetPositionIndex(int flag)
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
    private int FlagTarget_SetDefaultPositionIndex(int flag, int targetGroup, int targetCount, int min, int max)
    {
        if (targetCount == (int)ETargetCount.All)
        {
            switch ((ETargetGroup)targetGroup)
            {
                case ETargetGroup.Party: return (0b_0000_0111); ;
                case ETargetGroup.Enemy: return (0b_0111_1000); ;
                case ETargetGroup.ExceptSelf: return ~(1 << GameMgr.NowOrder); ;
            }
        }
        else if (targetCount == (int)ETargetCount.One)
        {
            switch ((ETargetGroup)targetGroup)
            {
                case ETargetGroup.Party:
                case ETargetGroup.Enemy:
                    {
                        Unit target;
                        for (int i = min; i < max; ++i)
                        {
                            target = UnitMgr.InBattle[i];
                            if (target != null && !target.IsFaint)
                            {
                                return (1 << i);
                            }
                        }
                    }
                    return flag;
                case ETargetGroup.Self:
                    return (1 << GameMgr.NowOrder);
            }
        }

        return flag;
    }

    public void Show(bool isOn)
    {
        obj.SetActive(isOn);
    }
}
public class UIBattle_Combo
{
    private GameObject obj;

    public UIBattle_Combo(Transform tf)
    {
        obj = tf.GetChild(2).gameObject;

        Show(false);
    }
    public void Show(bool isOn)
    {
        obj.SetActive(isOn);
    }
}