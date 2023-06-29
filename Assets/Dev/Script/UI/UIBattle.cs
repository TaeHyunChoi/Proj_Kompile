using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static UIBattle;
using static BIT;


public class UIBattle : MonoBehaviour
{
    public  static UIBattle Instance { get => instance; }
    private static UIBattle instance;

    private UIBattle_Menu       uiMenu;
    private UIBattle_Targeting  uiTarget;

    public static Unit NowUnit { get => UnitMgr.InBattle[GameMgr.NowOrder]; }

    //[Targeting][Menu][Content]
    private int select;

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
    }

    public void Active(int type, bool isOn)
    {
        //입력: 매개변수

        //처리: 입력모드
        InputMgr.SetMode(type);

        //처리: 기본 입력 처리 (마음에 안든다.)
        select = NowUnit.LastSelect;
        if (type == IDxSTATE.BATTLE_TARGET)
        {
            select = uiTarget.ProcInput(select, 0);
        }

        //출력: UI
        uiMenu.  Show(type == IDxSTATE.BATTLE_MENU   & isOn, select);
        uiTarget.Show(type == IDxSTATE.BATTLE_TARGET & isOn, select);
    }
    public void Input(byte type, int input)
    {
        switch (type)
        {
            case IDxSTATE.BATTLE_MENU:   select = uiMenu.  ProcInput(select, input);    break;
            case IDxSTATE.BATTLE_TARGET: select = uiTarget.ProcInput(select, input);    break;
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
            icon.sprite = ResourceMgr.SPIcon[rcsCode];
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
        {
            slots.Add(new UISlot_Battle(contentScroll.GetChild(i).gameObject));
        }
        contentTitleText = content.GetChild(1).GetComponentsInChildren<TextMeshProUGUI>();

        Show(false, 0);
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
        switch (input & IDxINPUT.INTERACT)
        {
            case IDxINPUT.ENTER:
                NowUnit.LastSelect_Update(select);
                switch (idxMenu)
                {
                    case IDxUI.BATTLE_MODE: ProcChangeMode();                break;
                    case IDxUI.BATTLE_ITEM: ProcUseItem();                   break;
                    default: Instance.Active(IDxSTATE.BATTLE_TARGET, true); break;
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
                if (idxMenu == (int)IDxUI.BATTLE_MAX - 1)
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
                    select = ((IDxUI.BATTLE_MAX - 1) << SHIFT_MENU); //CONTENT 초기화
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

        switch (menu)
        {
            case IDxUI.BATTLE_BASIC:
                text[0] = "기본기";
                text[1] = string.Empty;
                break;
            case IDxUI.BATTLE_SOLO:
                text[0] = "개인 공격기";
                text[1] = "MP";
                break;
            case IDxUI.BATTLE_GROUP:
                text[0] = "전체 공격기";
                text[1] = "MP";
                break;
            case IDxUI.BATTLE_MODE:
                text[0] = "모드";
                text[1] = string.Empty;
                break;
            case IDxUI.BATTLE_ITEM:
                text[0] = "아이템";
                text[1] = string.Empty;
                break;
            case IDxUI.BATTLE_SPECIAL:
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


    private int SlotText_GetSlotInfo(int select, out string[,] info)
    {
        info = new string[,] { };
        int menu = (select & MASK_NOW_MENU) >> SHIFT_MENU;
        int idxLast;

        switch (menu)
        {
            case IDxUI.BATTLE_BASIC:
                info = SlotText_GetSkillInfo(type: IDxSkill.BASIC, out idxLast);
                break;
            case IDxUI.BATTLE_SOLO:
                info = SlotText_GetSkillInfo(type: IDxSkill.SOLO, out idxLast);
                break;
            case IDxUI.BATTLE_GROUP:
                info = SlotText_GetSkillInfo(type: IDxSkill.GROUP, out idxLast);
                break;
            case IDxUI.BATTLE_MODE:
                info = SlotText_GetModeInfo(out idxLast);
                break;
            case IDxUI.BATTLE_ITEM:
                info = SlotText_GetItemInfo(out idxLast);
                break;
            case IDxUI.BATTLE_SPECIAL:
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
        select = SlotText_GetSlotInfo(select, out string[,] info);

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

        Show(false, 0);
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
            || skill.TargetGroupType == IDxSkill.TARGET_SELF
            || skill.TargetCountType == IDxSkill.TARGET_ALL)
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
                Instance.Active(IDxSTATE.BATTLE_MENU, true);
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
        if (targetCount == IDxSkill.TARGET_ALL)
        {
            switch (targetGroup)
            {
                case IDxSkill.TARGET_PARTY: return (0b_0000_0111); ;
                case IDxSkill.TARGET_ENEMY: return (0b_0111_1000); ;
                case IDxSkill.TARGET_XORSELF: return ~(1 << GameMgr.NowOrder); ;
            }
        }
        else if (targetCount == IDxSkill.TARGET_ONE)
        {
            switch (targetGroup)
            {
                case IDxSkill.TARGET_PARTY:
                case IDxSkill.TARGET_ENEMY:
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
                case IDxSkill.TARGET_SELF:
                    return (1 << GameMgr.NowOrder);
            }
        }

        return flag;
    }

    public void Show(bool isOn, int select)
    {
        obj.SetActive(isOn);
        if (!isOn)
        {
            return;
        }

        //select = ProcInput(select, 0);
        NowUnit.LastSelect_Update(select);
    }
}