using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using static UIBattle;
using System;
using UnityEngine.Windows;
using static UnityEngine.GraphicsBuffer;

public enum EIdxMENU : int
{
    SkillBasic = 0,
    SkillSolo,
    SkillGroup,
    Mode,
    Item,
    SkillSpecial,
    Count
}
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
    public  static UIBattle Instance { get => instance; }
    private static UIBattle instance;

    private UIBattle_Menu       uiMenu;
    private UIBattle_Targeting  uiTarget;
    private UIBattle_Combo      uiCombo;

    public const int MASK_IDX_LAST  = 0x00FF_0000;
    public const int MASK_MENU      = 0x0000_0F00;
    public const int MASK_CONTENT   = 0x0000_00FF;

    public const int SHIFT_IDX_LAST = 4 * 4;
    public const int SHIFT_MENU     = 4 * 2;
    //public static int SHIFT_CONTENT = 0; //사실상 사용X

    private int selectMenu;
    private int selectTarget;
    private int selectCombo;

    public static void Instantiate()
    {
        if (instance != null)
            return;

        GameObject obj = Resources.Load<GameObject>("Prefab/UIBattle");
        obj = Instantiate(obj, UIMgr.Canvas_Battle.transform);
        instance = obj.AddComponent<UIBattle>();
        instance.Init();
    }
    private void Init()
    {
        selectMenu = selectTarget = selectCombo = 0;

        uiMenu = new UIBattle_Menu(transform);
        uiTarget = new UIBattle_Targeting(transform);
        uiCombo = new UIBattle_Combo(transform);

        uiMenu.Show(false);
        uiTarget.Show(false, selectTarget);
        uiCombo.Show(false);
    }
    public void Active(int type, bool isOn)
    {
        //Get Unit`s Last Select : 함수 다시 만들어야 한다. selct가 menu, target, combo로 나뉨
        //int select = UnitMgr.Battle_GetUnit(GameMgr.NowOrder).LastSelect; 

        //Set Input (+ Mode)
        switch (type)
        {
            case 0:
                InputMgr.SetMode(IDxINPUT.BATTLE_MENU);
                selectMenu = uiMenu.ProcInput(selectMenu, 0);
                break;
            case 1:
                InputMgr.SetMode(IDxINPUT.BATTLE_TARGERT);
                selectTarget = uiTarget.ProcInput(selectTarget, 0);
                break;
            case 2:
                InputMgr.SetMode(IDxINPUT.BATTLE_COMBO);
                break;
        }

        //Active UI
        uiMenu.Show(type == IDxUI.BATTLE_MENU & isOn);
        uiTarget.Show(type == IDxUI.BATTLE_TARGET & isOn, selectTarget);
        uiCombo.Show(type == IDxUI.BATTLE_COMBO & isOn);
    }


    public void Input(byte type, int input)
    {
        switch (type)
        {
            case 0: selectMenu = uiMenu.ProcInput(selectMenu, input);       break;
            case 1: selectTarget = uiTarget.ProcInput(selectTarget, input); break;
        }
    }
    public void UpdateUI_Target(Unit[] units, Vector3 offset)
    {
        uiTarget.InitArrows(units, offset);   
    }
}
public class UIBattle_Menu
{
    private GameObject obj;

    private RectTransform   menuArrow;
    private RectTransform   contentArrow;

    private TextMeshProUGUI[]   contentTitleText;
    private GameObject          prefabSlot;
    private Transform           contentScroll;
    private List<UISlot_Battle> slots;

    private static Vector2  menuArrowDefault;
    private static Vector2  contentArrowDefault;
    private static float    deltaMenu = 150f;
    private static float    deltaContent = -125f;

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
    }
    public void Show(bool isOn)
    {
        obj.SetActive(isOn);
    }

    public int ProcInput(int select, int input)
    {
        select = Input(select, input);
        UpdateUI_Panel(ref select); //UpdateUI에서 select를 반환하니 오히려 가독성 떨어짐. ref로 처리
        UpdateUI_Arrow(select);

        return select;
    }
    private int Input(int select, int input)
    {
        int menu = (select & MASK_MENU) >> SHIFT_MENU;
        int idxLast = (select & MASK_IDX_LAST) >> SHIFT_IDX_LAST;

        //## Update Select
        switch (input & IDxINPUT.INTERACT)
        {
            case IDxINPUT.ENTER:
                {
                    switch ((EIdxMENU)menu)
                    {
                        case EIdxMENU.Mode: ProcChangeMode();                break;
                        case EIdxMENU.Item: ProcUseItem();                   break;
                        default:/*Battle*/  Instance.Active(type: 1, true);    break;
                    }

                    Debug.Log("Plz Save Unit Action");
                    //UnitMgr.Battle_SaveUnitAction(nowOrder, select);
                }
                return select;
            case IDxINPUT.CANCEL:
                {

                }
                return select;
            case IDxINPUT.OPTION:
                {

                }
                return select;
        }
        switch (input & IDxINPUT.DIRECTION)
        {
            case IDxINPUT.RIGHT:
                //마지막 메뉴?
                if (menu == (int)EIdxMENU.Count - 1)
                {
                    select = 0; //MENU 초기화 → MENU =0, CONTENT 초기화 (TARGET만 남는다)
                    break;
                }

                select += (1 << SHIFT_MENU);
                select &= ~MASK_CONTENT;        //MENU 변경 → CONTENT 초기화
                break;
            case IDxINPUT.LEFT:

                //맨앞의 메뉴?
                if (menu == 0)
                {
                    select = (((int)EIdxMENU.Count - 1) << SHIFT_MENU); //CONTENT 초기화
                    break;
                }

                select -= (1 << SHIFT_MENU);
                select &= ~MASK_CONTENT;   //MENU 변경 → CONTENT 초기화
                break;
            case IDxINPUT.DOWN:
                if ((select & MASK_CONTENT) == idxLast)
                    select &= ~MASK_CONTENT; //CONTENT 초기화
                else
                    select += 0x01;
                break;
            case IDxINPUT.UP:
                if ((select & MASK_CONTENT) == 0x00)
                    select |= idxLast;
                else
                    select -= 0x01;
                break;
        }

        return select;
    }
    private void UpdateUI_Panel(ref int select)
    {
        string[] text = new string[2];
        string[,] code = new string[,] { };
        int loadCount = 0;
        int menu = (select & MASK_MENU) >> SHIFT_MENU;

        switch ((EIdxMENU)menu)
        {
            case EIdxMENU.SkillBasic:
                text[0] = "기본기";
                text[1] = string.Empty;
                code = ContentSlot_SetSkill(type: IDxSkill.BASIC, out loadCount);
                break;
            case EIdxMENU.SkillSolo:
                text[0] = "개인 공격기";
                text[1] = "MP";
                code = ContentSlot_SetSkill(type: IDxSkill.SOLO, out loadCount);
                break;
            case EIdxMENU.SkillGroup:
                text[0] = "전체 공격기";
                text[1] = "MP";
                code = ContentSlot_SetSkill(type: IDxSkill.GROUP, out loadCount);
                break;
            case EIdxMENU.Mode:
                text[0] = "모드";
                text[1] = string.Empty;
                code = ContentSlot_SetMode(out loadCount);
                break;
            case EIdxMENU.Item:
                text[0] = "아이템";
                text[1] = string.Empty;
                code = ContentSlot_SetItem(out loadCount);
                break;
            case EIdxMENU.SkillSpecial:
                text[0] = "특수기";
                text[1] = string.Empty;
                code = ContentSlot_SetSkill(type: IDxSkill.SPECIAL, out loadCount);
                break;
        }

        contentTitleText[0].text = text[0];
        contentTitleText[1].text = text[1];

        //Use Slot => New or Active(true)
        int i = 0;
        for (; i < loadCount; ++i)
        {
            if (i >= slots.Count)
            {
                GameObject slot = GameObject.Instantiate(prefabSlot, contentScroll);
                slots.Add(new UISlot_Battle(slot));
            }

            slots[i].Load(code[0, i], code[1, i]);
        }

        //Not Used Slot => Active(false) : 앞선 i번째부터 이어서 체크하는구나
        for (; i < slots.Count; ++i)
            slots[i].SetActive(false);
        
        select &= ~MASK_IDX_LAST;
        select |= ((loadCount - 1) << SHIFT_IDX_LAST);
    }
    private void UpdateUI_Arrow(int select)
    {
        int menu    = (select & MASK_MENU) >> SHIFT_MENU; ;
        int content = (select & MASK_CONTENT);

        menuArrow.anchoredPosition = menuArrowDefault + menu * new Vector2(deltaMenu, 0);
        contentArrow.anchoredPosition = contentArrowDefault + content * new Vector2(0, deltaContent);
    }

    private string[,] ContentSlot_SetSkill(int type, out int count)
    {
        List<SkillData> skills = UnitMgr.Battle_GetSkillTypeof(GameMgr.NowOrder, type);
        string[,] code = new string[2, skills.Count];
        count = code.GetLength(1);

        for (int i = 0; i < count; i++)
        {
            code[0, i] = skills[i].Name;
            code[1, i] = skills[i].RscCode;
        }

        return code;
    }
    private string[,] ContentSlot_SetMode(out int count)
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
    private string[,] ContentSlot_SetItem(out int count)
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


    //여기서부터는 다시 UIBattle(Layer)에게 넘겨 요청한다.
    private void ProcChangeMode()
    {

    }
    private void ProcUseItem()
    {

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
    }

    public int ProcInput(int select, int input)
    {
        select = Input(select, input);
        UpdateUI(select);

        return select;
    }

    public int Input(int select, int input)
    {
        //select는 [0b_0111_1111] 형태이다.

        //input => Get Data
        SkillData skill = UnitMgr.Battle_GetSkill(GameMgr.NowOrder, (select & MASK_MENU) >> SHIFT_MENU, (select & MASK_CONTENT));
        int min = (skill.TargetGroupType == 1) ? 3 : 0;
        int max = (skill.TargetGroupType == 1) ? 6 : 2;

        min = 1 << min;
        max = 1 << max;

        //input & data => select_exceptional
        if (select == 0
            || skill.TargetGroupType == (int)ETargetGroup.Self
            || skill.TargetCountType == (int)ETargetCount.All)
        {
            //기본으로 세팅된 입력값(Self, All은 계산 방법이 항상 동일하다.)
            select = TargetingInput_Default(select, skill.TargetGroupType, skill.TargetCountType, FlagToIndex(min), FlagToIndex(max));

            //입력처리를 스킵하지만 + UI Update는 가능하다.
            input = 0;
        }

        //input & data => select
        Unit target;
        switch (input & IDxINPUT.INTERACT)
        {
            case IDxINPUT.ENTER:
                {

                }
                return select;
            case IDxINPUT.CANCEL:
                {

                }
                return select;
            case IDxINPUT.OPTION:
                {

                }
                return select;
        }
        switch (input & IDxINPUT.DIRECTION)
        {
            case IDxINPUT.UP:
                while (true)
                {
                    if (select == min)
                        select = max;
                    else
                        select >>= 1;

                    int idxPos = FlagToIndex(flag: select);
                    target = UnitMgr.Battle_GetUnit(idxPos);
                    if (target != null && !target.IsFaint)
                        break;
                }
                break;
            case IDxINPUT.DOWN:
                while (true)
                {
                    if (select == max)
                        select = min;
                    else
                        select <<= 1;

                    int idxPos = FlagToIndex(flag: select);
                    target = UnitMgr.Battle_GetUnit(idxPos);
                    if (target != null && !target.IsFaint)
                        break;
                }
                break;
        }

        //select => ui
        //UpdateUI(select);

        return select;
    }

    private int FlagToIndex(int flag)
    {
        for (int i = 0; i < 7; ++i)
        {
            if ((flag >> i) == 1)
                return i;
        }

        return 0;
    }
    private int TargetingInput_Default(int select, int targetGroup, int targetCount, int min, int max)
    {
        if (targetCount == (int)ETargetCount.All)
        {
            switch ((ETargetGroup)targetGroup)
            {
                case ETargetGroup.Party:        return (0b_0000_0111); ;
                case ETargetGroup.Enemy:        return (0b_0111_1000); ;
                case ETargetGroup.ExceptSelf:   return ~(1 << GameMgr.NowOrder); ;
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
                            target = UnitMgr.Battle_GetUnit(i);
                            if (target != null && !target.IsFaint)
                            {
                                return (1 << i);
                            }
                        }
                    }
                    return select;
                case ETargetGroup.Self:
                    return (1 << GameMgr.NowOrder);
            }
        }

        return select;
    }
    public void UpdateUI(int select)
    {
        //지금 selectTarget이 들어왔음 [0b_0111_1111]

        Unit unit;
        Vector3 rect;

        int comp;
        for (int i = 0; i < 7; ++i)
        {
            comp = select & (1 << i);
            comp >>= i;

            if (comp == 0)
            {
                targetingArrows[i].gameObject.SetActive(false);
                continue;
            }

            unit = UnitMgr.Battle_GetUnit(i);
            rect = CameraMgr.Battle_ScreenToLocalInRect(unit.Pos + unit.transform.up); //나중에 unit height를 곱하던가...
            targetingArrows[i].localPosition = rect;
            targetingArrows[i].gameObject.SetActive(true);
        }
    }

    public void InitArrows(Unit[] units, Vector3 offset)
    {
        Vector3 rect;
        for (int i = 0; i < units.Length; ++i)
        {
            if (units[i] == null)
                continue;

            rect = CameraMgr.Battle_ScreenToLocalInRect(units[i].Pos + offset);
            targetingArrows[i].localPosition = rect;
        }
    }
    public void Show(bool isOn, int select)
    {
        obj.SetActive(isOn);

        if(isOn)
            UpdateUI(select);
    }
}
public class UIBattle_Combo
{
    //private int menu        { get => (selectMenu & MASK_MENU) >> SHIFT_MENU; }
    //private int content     { get => (selectMenu & MASK_CONTENT); }
    private GameObject obj;

    public UIBattle_Combo(Transform tf)
    {
        obj = tf.GetChild(2).gameObject;
    }
    public void Show(bool isOn)
    {
        obj.SetActive(isOn);
    }
}