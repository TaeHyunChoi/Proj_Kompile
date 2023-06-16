using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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
    private static int MASK_TARGET  = 0x00FF_0000;
    private static int MASK_MENU    = 0x0000_0F00;
    private static int MASK_CONTENT = 0x0000_00FF;

    private static int SHIFT_TARGET    = 4 * 4;
    private static int SHIFT_MENU      = 4 * 2;
    //private static int SHIFT_CONTENT = 0; //사실상 사용X
    #endregion

    private int select;         //Total Input Value
    private int menu        { get => (select & MASK_MENU) >> SHIFT_MENU; }
    private int content     { get => (select & MASK_CONTENT)/* >> SHIFT_CONTENT*/; }
    private int targeting   { get => (select & MASK_TARGET) >> SHIFT_TARGET; } //[0-2:Party][3-6:Enemy]

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

        menuArrow.anchoredPosition = menuArrowDefault + menu * new Vector2(deltaMenu, 0);
        contentArrow.anchoredPosition = contentArrowDefault + content * new Vector2(0, deltaContent);
        ContentUI_Update();
        InputMgr.SetMode(IDxINPUT.BATTLE_MENU);
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


    //## Input => Update Content
    public  void Input_Select(int input)
    {
        //## Update Select
        switch (input & IDxINPUT.INTERACT)
        {
            case IDxINPUT.ENTER:
                {
                    switch ((EIdxMENU)menu)
                    {
                        case EIdxMENU.SkillBasic:   TargetingProc(IDxSkill.BASIC);      break;
                        case EIdxMENU.SkillSolo:    TargetingProc(IDxSkill.SOLO);       break;
                        case EIdxMENU.SkillGroup:   TargetingProc(IDxSkill.GROUP);      break;
                        case EIdxMENU.SkillSpecial: TargetingProc(IDxSkill.SPECIAL);    break;
                        case EIdxMENU.Mode:         ProcUI_ChangeMode();                break;
                        case EIdxMENU.Item:         ProcUI_UseItem();                   break;
                    }

                    //끄응.. 이런 식의 정보 저장도 별로네...
                    Debug.Log("Plz Save Unit Action");
                    //UnitMgr.Battle_SaveUnitAction(nowOrder, select);
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
                //마지막 메뉴?
                if (menu == (int)EIdxMENU.Count - 1)
                {
                    select &= (0 | MASK_TARGET); //MENU 초기화 → MENU =0, CONTENT 초기화 (TARGET만 남는다)
                    break;
                }

                select += (1 << SHIFT_MENU);
                select &= ~MASK_CONTENT;        //MENU 변경 → CONTENT 초기화
                break;
            case IDxINPUT.LEFT:
                
                    //맨앞의 메뉴?
                    if (menu == 0)
                    {
                        select = (((int)EIdxMENU.Count - 1) << SHIFT_MENU | targeting); //CONTENT 초기화
                        break;
                    }

                    select -= (1 << SHIFT_MENU);
                    select &= ~MASK_CONTENT;   //MENU 변경 → CONTENT 초기화
                
                break;
            case IDxINPUT.DOWN:
                
                    if ((select & MASK_CONTENT) == lastSlotIndex)
                        select &= ~MASK_CONTENT; //CONTENT 초기화
                    else
                        select += 0x01;
                
                break;
            case IDxINPUT.UP:
                    if ((select & MASK_CONTENT) == 0x00)
                        select |= lastSlotIndex;
                    else
                        select -= 0x01;
                break;
        }

        //## Update Arrow
        menuArrow.anchoredPosition = menuArrowDefault + menu * new Vector2(deltaMenu, 0);
        contentArrow.anchoredPosition = contentArrowDefault + content * new Vector2(0, deltaContent);

        //## Update Content Panel
        ContentUI_Update();
    }
    private void ContentUI_Update() //메뉴판 교체
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
    private string[,] ContentSlot_SetSkill(int type, out int count)
    {
        List<SkillData> skills = UnitMgr.Battle_GetSkillTypeof(nowOrder, type);
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


    //## Input => Update Target
    private void TargetingProc(int type)
    {
        //## 입력
        SkillData skill = UnitMgr.Battle_GetSkill(nowOrder, type, content);

        //## 처리
        //여기서 이제 타겟 정보를 가져와서 이러쿵 저러쿵 한다는건데...
        //1. 이전에 저장된 

        InputMgr.SetMode(IDxINPUT.BATTLE_TARGERT);  //입력모드: Targeting으로 설정


        //## 출력(표시)
        Show(false);                //Close Content Panel
        TargetingUI_Update(skill);  //Targeting Arrow
    }
    public void Input_Targeting(int input)
    {
        //[입력] 범위 제한 (min, max)
        SkillData skill = UnitMgr.Battle_GetSkill(nowOrder, menu, content);

        //[입력] input => select
        int before = targeting;
        if (targeting == 0)
            TargetingInput_Defalut(skill.TargetGroupType, skill.TargetCountType);

        Debug.Log($"[{skill.Name}] IDxTarget: {before} => {(select & MASK_TARGET) >> SHIFT_TARGET}");
        return;

        //[처리] select => skill


        //[출력] skill => ui
        TargetingUI_Update(skill);
    }
    private int SelectTargetDefault(SkillData skill)
    {


        return -1;
    }
    private void TargetingUI_Update(SkillData skill)
    {
        Debug.Log("Dev ing: TargetingUI_Update;");

    }
    private void TargetingInput_Defalut(int group, int count)
    {
        int flagTarget = 0;

        if (count == (int)ETargetCount.All)
        {
            switch ((ETargetGroup)group)
            {
                case ETargetGroup.Party:
                    select |= (0b_0111_0000) << SHIFT_TARGET;
                    return;
                case ETargetGroup.Enemy:
                    select |= (0b_0000_1111) << SHIFT_TARGET;
                    return;
                case ETargetGroup.ExceptSelf:
                    flagTarget = ~(1 << nowOrder);
                    flagTarget <<= SHIFT_TARGET;
                    select = (flagTarget | menu | content);
                    return;
            }
        }
        else if (count == (int)ETargetCount.One)
        {
            switch ((ETargetGroup)group)
            {
                case ETargetGroup.Party:
                case ETargetGroup.Enemy:
                    {
                        //test 중이니까 일단 이렇게. 아 enum 개불편하네 진짜;;;
                        int min = (group == 1) ? 3 : 0;
                        int max = (group == 1) ? 6 : 2;

                        //[★★★★★]이거 분명이 헷갈릴텐데? 조치 필요
                        //인덱스 수정 필요
                        for (int i = min; i < max; ++i)
                        {
                            Unit target = UnitMgr.Battle_GetUnit(i);
                            if (target != null && !target.IsFaint)
                            {
                                flagTarget = (1 << (6 - i) + SHIFT_TARGET);
                                select = (flagTarget | menu | content);
                                Debug.Log($"flag:{Mathf.Log(flagTarget >> SHIFT_TARGET, 2)} => targeting:{Mathf.Log(targeting, 2)}");
                                //idx는 targeting에서 한 번 더 변환이 필요하구나;

                                return;
                            }
                        }
                    }
                    return;
                case ETargetGroup.Self:
                    {
                        flagTarget = (1 << nowOrder + SHIFT_TARGET);
                        select = (flagTarget | menu | content);
                    }
                    return;
            }
        }

        //탐색이 안되면 그냥 target_null로 치자.
        select = (flagTarget | menu | content);
    }
    private void TargetingInput_One(int group)
    {
        switch ((ETargetGroup)group)
        {
            case ETargetGroup.Party:
                { 
                    
                }
                break;
            case ETargetGroup.Enemy:
                break;
            case ETargetGroup.Self:
                break;
        }
    }
    private void TargetingInput_All(int group)
    {
        switch ((ETargetGroup)group)
        {
            case ETargetGroup.Party:
                break;
            case ETargetGroup.Enemy:
                break;
            case ETargetGroup.Self:
                break;
        }
    }


    private void ProcUI_ChangeMode()
    { 
        
    }
    private void ProcUI_UseItem()
    { 
        
    }
}