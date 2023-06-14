using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIBattleSelect_2 : MonoBehaviour
{
    public static UIBattleSelect_2 Instance { get => instance; }
    private static UIBattleSelect_2 instance;

    private enum EIdxMENU : int
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
            go =    _go;
            icon =  _go.transform.GetChild(0).GetComponent<Image>();
            name =  _go.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        }
        public void Load(string slotName, string rcsCode)
        {
            name.text   = slotName;
            icon.sprite = ResourceMgr.SPIcon[rcsCode];
            go.SetActive(true);
        }
        public void SetActive(bool on)
        {
            go.SetActive(on);
        }
    }

    #region UI
    private TextMeshProUGUI[] contentTitleText;
    private GameObject prefabSlot;
    private Transform contentScroll;
    private List<UISlot_Battle> slots;

    private RectTransform   menuArrow;
    private RectTransform   contentArrow;
    private RectTransform[] targetingArrows;

    private static Vector2 menuArrowDefault;
    private static Vector2 contentArrowDefault;
    private static float deltaMenu = 150f;
    private static float deltaContent = -125f;
    #endregion
    #region BitMask
    private static int MASK_TARGET_SOLO     = 0xF000;
    private static int MASK_MENU            = 0x0F00;
    private static int MASK_CONTENT         = 0x00FF;

    private static int SHIFT_TARGET_SOLO    = 4 * 3;
    private static int SHIFT_MENU           = 4 * 2;
    //private static int SHIFT_CONTENT = 0; //사실상 사용X
    #endregion

    private int select;         //Total Input Value
    private int menu        { get => (select & MASK_MENU) >> SHIFT_MENU; }
    private int content     { get => (select & MASK_CONTENT)/* >> SHIFT_CONTENT*/; }
    private int targetSolo  { get => (select & MASK_TARGET_SOLO) >> SHIFT_TARGET_SOLO; }

    private int lastSlotIndex;  //선택 가능한 마지막 인덱스

    private int nowOrder { get => GameMgr.NowOrder; }


    //Instantiate => Show
    public static void Instantiate()
    {
        if (instance != null)
            return;

        GameObject go = Resources.Load<GameObject>("Prefab/UIBattleMenu");
        go = Instantiate(go, UIMgr.Canvas_Battle.transform);
        instance = go.AddComponent<UIBattleSelect_2>();

        //instance.~~ 붙이기 싫어서 걍 Init()으로 묶음
        instance.Init();
    }
    private void Init()
    {
        Transform menu = transform.GetChild(0).GetChild(0);
        menuArrow = menu.GetChild(3).GetComponent<RectTransform>();
        menuArrowDefault = menuArrow.anchoredPosition;

        Transform content = transform.GetChild(0).GetChild(1);
        contentArrow = content.GetChild(3).GetComponent<RectTransform>();
        contentArrowDefault = contentArrow.anchoredPosition;

        slots = new List<UISlot_Battle>();
        contentScroll = content.GetChild(2).GetChild(0).GetChild(0);
        for (int i = 0; i < contentScroll.childCount; ++i)
            slots.Add(new UISlot_Battle(contentScroll.GetChild(i).gameObject));

        contentTitleText = content.GetChild(1).GetComponentsInChildren<TextMeshProUGUI>();

        //아 마음에 안드네ㅡㅡ
        RectTransform[] temp = transform.GetChild(1).GetComponentsInChildren<RectTransform>(true);
        targetingArrows = new RectTransform[temp.Length - 1];
        for (int i = 1; i < temp.Length; ++i)
            targetingArrows[i - 1] = temp[i].GetComponent<RectTransform>();

        prefabSlot = ResourceMgr.Prefab["UIBattleSkill"];
        instance.Show(false);
    }
    public void Show(bool on)
    {
        gameObject.SetActive(on);
        if (!on)
            return;

        //[HOLD] Get Unit`s Last Select(Act)
        select = 0;

        menuArrow.anchoredPosition = menuArrowDefault + menu * new Vector2(deltaMenu, 0);
        contentArrow.anchoredPosition = contentArrowDefault + content * new Vector2(0, deltaContent);
        UpdateUI_Content();
        InputMgr.SetMode(IDxINPUT.BATTLE_MENU);
    }
    public void InitArrows(List<Unit> units, Vector3 offset)
    {
        Vector3 rect;
        for (int i = 0; i < units.Count; ++i)
        {
            rect = CameraMgr.Battle_ScreenToLocalInRect(units[i].Pos + offset);
            targetingArrows[i].localPosition = rect;
        }
    }


    //## Input => Update Content
    public  void InputUI_Select(int input)
    {
        //## Update Select
        switch (input & IDxINPUT.INTERACT)
        {
            case IDxINPUT.ENTER:
                {
                    switch ((EIdxMENU)menu)
                    {
                        case EIdxMENU.SkillBasic:   ProcUI_Targeting(IDxSkill.BASIC);   break;
                        case EIdxMENU.SkillSolo:    ProcUI_Targeting(IDxSkill.SOLO);    break;
                        case EIdxMENU.SkillGroup:   ProcUI_Targeting(IDxSkill.GROUP);   break;
                        case EIdxMENU.SkillSpecial: ProcUI_Targeting(IDxSkill.SPECIAL); break;
                        case EIdxMENU.Mode:         ProcUI_ChangeMode();                break;
                        case EIdxMENU.Item:         ProcUI_UseItem();                   break;
                    }

                    //끄응.. 이런 식의 정보 저장도 별로네...
                    UnitMgr.Battle_SaveUnitAction(nowOrder, select);
                }
                return;
            case IDxINPUT.CANCEL:
                {

                }
                return;
            case IDxINPUT.OPTION:
                {

                }
                return;
        }
        switch (input & IDxINPUT.DIRECTION)
        {
            case IDxINPUT.RIGHT:
                {
                    //마지막 메뉴?
                    if (menu == (int)EIdxMENU.Count - 1)
                    {
                        select = 0;
                        break;
                    }

                    select += (1 << SHIFT_MENU);
                    select &= MASK_MENU;   //MENU 변경 → TARGET, CONTENT 초기화
                }
                break;
            case IDxINPUT.LEFT:
                {
                    //맨앞의 메뉴?
                    if (menu == 0)
                    {
                        select = ((int)EIdxMENU.Count - 1) << SHIFT_MENU;
                        break;
                    }

                    select -= (1 << SHIFT_MENU);
                    select &= MASK_MENU;   //MENU 변경 → TARGET, CONTENT 초기화
                }
                break;
            case IDxINPUT.DOWN:
                {
                    if ((select & MASK_CONTENT) == lastSlotIndex)
                        select &= ~MASK_CONTENT; //CONTENT 초기화
                    else
                        select += 0x01;
                }
                break;
            case IDxINPUT.UP:
                {
                    if ((select & MASK_CONTENT) == 0x00)
                        select |= lastSlotIndex;
                    else
                        select -= 0x01;
                }
                break;
        }

        //## Update Arrow
        menuArrow.anchoredPosition = menuArrowDefault + menu * new Vector2(deltaMenu, 0);
        contentArrow.anchoredPosition = contentArrowDefault + content * new Vector2(0, deltaContent);

        //## Update Content Panel
        UpdateUI_Content();
    }
    private void UpdateUI_Content() //메뉴판 교체
    {
        string[] text = new string[2];
        string[,] code = new string[,] { };
        int loadCount = 0;

        switch ((EIdxMENU)menu)
        {
            case EIdxMENU.SkillBasic:
                text[0] = "기본기";
                text[1] = string.Empty;
                code = GetSlotData_Skill(type: IDxSkill.BASIC, out loadCount);
                break;
            case EIdxMENU.SkillSolo:
                text[0] = "개인 공격기";
                text[1] = "MP";
                code = GetSlotData_Skill(type: IDxSkill.SOLO, out loadCount);
                break;
            case EIdxMENU.SkillGroup:
                text[0] = "전체 공격기";
                text[1] = "MP";
                code = GetSlotData_Skill(type: IDxSkill.GROUP, out loadCount);
                break;
            case EIdxMENU.Mode:
                text[0] = "모드";
                text[1] = string.Empty;
                code = GetSlotData_Mode(out loadCount);
                break;
            case EIdxMENU.Item:
                text[0] = "아이템";
                text[1] = string.Empty;
                code = GetSlotData_Item(out loadCount);
                break;
            case EIdxMENU.SkillSpecial:
                text[0] = "특수기";
                text[1] = string.Empty;
                code = GetSlotData_Skill(type: IDxSkill.SPECIAL, out loadCount);
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
                GameObject slot = Instantiate(prefabSlot, contentScroll);
                slots.Add(new UISlot_Battle(slot));
            }

            slots[i].Load(code[0, i], code[1, i]);
        }

        //Not Used Slot => Active(false) : 앞선 i번째부터 이어서 체크하는구나
        for (; i < slots.Count; ++i)
            slots[i].SetActive(false);

        //Update Window Content Max Index
        lastSlotIndex = loadCount - 1;
    }
    private string[,] GetSlotData_Skill(int type, out int count)
    {
        List<SkillData> skills = UnitMgr.Battle_GetSkillTypeof(nowOrder, type);
        string[,] code = new string[2, skills.Count];
        count = code.GetLength(1);

        for (int i = 0; i < count; i++)
        {
            code[0, i] = skills[i].Name;
            code[1, i] = skills[i].RcsCode;
        }

        return code;
    }
    private string[,] GetSlotData_Mode(out int count)
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
    private string[,] GetSlotData_Item(out int count)
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


    //## Input => Update Target
    private void ProcUI_Targeting(int type)
    {
        //## 입력
        //Queue로 현재의 Unit 참조 가능 => now order 삭제하는 것도 가능하겠는데?

        SkillData skillSelected = UnitMgr.Battle_GetSkill(nowOrder, type, content);

        //## 처리



        InputMgr.SetMode(IDxINPUT.BATTLE_TARGERT);  //입력모드: Targeting으로 설정


        //## 출력(표시)
        Show(false);    //Close Content Panel
                        //Targeting Arrow
    }
    private void InputUI_Targeting(int input)
    {
        SkillData skill = UnitMgr.Battle_GetSkill(nowOrder, menu, content);

        //최초 몇 명을 가리켜야 하는가?
        //이전 정보는 아직 없는 셈으로 친다...
        //타겟수가 몇 명이냐 => 이것도 비트마스킹 바꿔서 다르게 처리해야 하네;

        //int MASK_TARGET = 0x000F_F000; (총 8칸)


        //타겟을 어떻게 표시할 것이냐
        //(1) 자료 구조가 바뀌어 데이터 처리 방식도 바꿔야 함
        //(2) UI를 Unit이 아닌 Target에게 옮겨야 함 (오우야;)
        //선택한 타겟 정보를 어디에, 어떻게 저장할 것이냐 ()

        #region before
        /*
        bool isSoloTarget = (skill.TargetGroup != IDxUNIT.TARGET_PLY_SOLO || skill.TargetGroup != IDxUNIT.TARGET_ENM_SOLO);
        switch (input & IDxINPUT.INTERACT)
        {
            case IDxINPUT.ENTER:
                {
                    UnitMgr.Battle_ActUnit(nowOrder, skill, select);
                    InputMgr.SetMode(IDxINPUT.BATTLE_COMBO);

                    Reset_Target();
                    Show(false);
                }
                return;
            case IDxINPUT.CANCEL:
                {
                    InputMgr.SetMode(IDxINPUT.BATTLE_MENU);
                    Reset_Target();
                }
                return;
        }
        switch (input & IDxINPUT.DIRECTION)
        {
            case IDxINPUT.UP:
            case IDxINPUT.LEFT:
                {
                    if (!isSoloTarget)
                        return;

                    if (selectTargetOne == 0x00)
                        select |= (indexENMMax << shiftTargetOne);
                    else
                        select -= (1 << shiftTargetOne);
                }
                break;
            case IDxINPUT.DOWN:
            case IDxINPUT.RIGHT:
                {
                    if (!isSoloTarget)
                        return;

                    if (selectTargetOne == indexENMMax)
                        select &= ~maskTargetOne;
                    else
                        select += (1 << shiftTargetOne);
                }
                break;
        }
        UnitMgr.Battle_SetTarget(skill.TargetGroup, selectTargetOne);
        //*/
        #endregion
    }
    private void UpdateUI_Targeting()
    {

    }


    private void ProcUI_ChangeMode()
    { 
        
    }
    private void ProcUI_UseItem()
    { 
        
    }
}