using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using static UIBattle;

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
    
    #region BitMask
    public const int MASK_MENU    = 0x0000_0F00;
    public const int MASK_CONTENT = 0x0000_00FF;
    public const int SHIFT_MENU   = 4 * 2;
    //public static int SHIFT_CONTENT = 0; //사실상 사용X
    #endregion

    private int selectMenu;
    private int selectTarget;

    public  static int IDxLastSlot { get => idxLastSlot; }
    private static int idxLastSlot;  //선택 가능한 마지막 인덱스

    //Instantiate => Show
    public static void Instantiate()
    {
        if (instance != null)
            return;

        GameObject go = Resources.Load<GameObject>("Prefab/UIBattle");
        go = Instantiate(go, UIMgr.Canvas_Battle.transform);
        instance = go.AddComponent<UIBattle>();

        //instance.~~ 붙이기 싫어서 걍 Init()으로 묶음
        instance.Init();
    }
    public void Show(int type, bool isOn)
    {
        //Get Unit`s Last Select(Act)
        int select = UnitMgr.Battle_GetUnit(GameMgr.NowOrder).LastSelect;
        uiMenu.Input(selectMenu = select, 0);
        InputMgr.SetMode(IDxINPUT.BATTLE_MENU);

        uiMenu.Show(type == IDxUI.BATTLE_MENU & isOn);
        uiTarget.Show(type == IDxUI.BATTLE_TARGET & isOn);
    }
    private void Init()
    {
        uiMenu   = new UIBattle_Menu(transform);
        uiTarget = new UIBattle_Targeting(transform);

        uiMenu.Show(true);
    }

    public void Input(byte type, int input)
    {
        if (type == 0)
            selectMenu = uiMenu.Input(selectMenu, input);
        else if (type == 1)
            selectTarget = uiTarget.Input(selectTarget, input);
        else if (type == 2)
        { 
            //콤보 관련된 기능을 넣으면 되겠지...
        }
    }
    public void UpdateUI_Target(Unit[] units, Vector3 offset)
    {
        uiTarget.InitArrows(units, offset);   
    }
    public void Update_IDxLastSlot(int index)
    {
        idxLastSlot = index;
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

    public int Input(int select, int input)
    {
        int menu    = (select & MASK_MENU) >> SHIFT_MENU;
        int content = (select & MASK_CONTENT);

        //## Update Select
        switch (input & IDxINPUT.INTERACT)
        {
            case IDxINPUT.ENTER:
                {
                    switch ((EIdxMENU)menu)
                    {
                        case EIdxMENU.Mode: ProcChangeMode(); break;
                        case EIdxMENU.Item: ProcUseItem();    break;
                        default: ProcTargeting();             break;    //이외는 모두 전투 관련 선택지
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

                if ((select & MASK_CONTENT) == IDxLastSlot)
                    select &= ~MASK_CONTENT; //CONTENT 초기화
                else
                    select += 0x01;

                break;
            case IDxINPUT.UP:
                if ((select & MASK_CONTENT) == 0x00)
                    select |= IDxLastSlot;
                else
                    select -= 0x01;
                break;
        }

        //## Update Arrow
        menuArrow.anchoredPosition    = menuArrowDefault + menu * new Vector2(deltaMenu, 0);
        contentArrow.anchoredPosition = contentArrowDefault + content * new Vector2(0, deltaContent);

        //## Update Content Panel
        UpdateUI(menu);

        return select;
    }
    public void UpdateUI(int menu) //메뉴판 교체?
    {
        string[] text = new string[2];
        string[,] code = new string[,] { };
        int loadCount = 0;

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

        Instance.Update_IDxLastSlot(loadCount - 1);
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
    private void ProcTargeting()
    {
        //처리
        InputMgr.SetMode(IDxINPUT.BATTLE_TARGERT);  //입력모드: Targeting으로 설정
        Instance.Input(type: 1, input:0);

        //이야~ 로직 심상치 않은데~!!

        //출력(표시)
        Instance.Show(type: 0, false);
        Instance.Show(type: 1, true);
    }
    private void ProcChangeMode()
    {

    }
    private void ProcUseItem()
    {

    }

    public void Show(bool isOn)
    {
        obj.SetActive(isOn);
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
            targetingArrows[i - 1] = temp[i].GetComponent<RectTransform>();
    }
    public int Input(int select, int input)
    {
        //입력 포맷은 [0b_0111_1111] 형태이다.
        int menu = (select & MASK_MENU) >> SHIFT_MENU;
        int content = (select & MASK_CONTENT);

        //input => Get Data
        SkillData skill = UnitMgr.Battle_GetSkill(GameMgr.NowOrder, menu, content);
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
                {
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
                }
                break;
            case IDxINPUT.DOWN:
                {
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
                }
                break;
        }

        //select => ui
        TargetingUI_Update();

        return select;
    }
    private int  FlagToIndex(int flag)
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
    private void TargetingUI_Update()
    {
        //1. 화살표 UI는 위치를 초기화 했는가
        //2. 화살표 UI는 어디에 저장되어 있는가
        //targetingArrows[] 변수에 좌표 초기화 후 저장까지

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
    public void Show(bool isOn)
    {
        obj.SetActive(isOn);
    }
}
public class UIBattle_Combo
{
    //private int menu        { get => (selectMenu & MASK_MENU) >> SHIFT_MENU; }
    //private int content     { get => (selectMenu & MASK_CONTENT); }
}