using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Mono.Cecil;
using static Player;

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

    #region UI
    private TextMeshProUGUI[] contentTitleText;
    private GameObject prefabSlot;
    private Transform contentScroll;
    private List<UISlot_Battle> slots;

    private RectTransform menuArrow;
    private RectTransform contentArrow;
    private RectTransform[] targetingArrows;

    private static Vector2 menuArrowDefault;
    private static Vector2 contentArrowDefault;
    private static float deltaMenu = 150f;
    private static float deltaContent = -125f;
    #endregion
    #region BitMask
    //private static int MASK_TARGET  = 0x00FF_0000;
    private static int MASK_MENU    = 0x0000_0F00;
    private static int MASK_CONTENT = 0x0000_00FF;

    //private static int SHIFT_TARGET = 4 * 4;
    private static int SHIFT_MENU   = 4 * 2;
    //private static int SHIFT_CONTENT = 0; //사실상 사용X
    #endregion

    private int selectMenu;
    private int selectTarget;

    private int menu        { get => (selectMenu & MASK_MENU) >> SHIFT_MENU; }
    private int content     { get => (selectMenu & MASK_CONTENT)/* >> SHIFT_CONTENT*/; }
    //private int targeting   { get => (selectMenu & MASK_TARGET) >> SHIFT_TARGET; } //[0-2:Party][3-6:Enemy]

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
    public void Input_Select(int input)
    {
        //## Update Select
        switch (input & IDxINPUT.INTERACT)
        {
            case IDxINPUT.ENTER:
                {
                    switch ((EIdxMENU)menu)
                    {
                        case EIdxMENU.Mode:  ProcUI_ChangeMode();    break;
                        case EIdxMENU.Item:  ProcUI_UseItem();       break;
                        default:             ProcTargeting();        break;    //이외는 모두 전투 관련 선택지
                    }

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
                    selectMenu = 0; //MENU 초기화 → MENU =0, CONTENT 초기화 (TARGET만 남는다)
                    break;
                }

                selectMenu += (1 << SHIFT_MENU);
                selectMenu &= ~MASK_CONTENT;        //MENU 변경 → CONTENT 초기화
                break;
            case IDxINPUT.LEFT:

                //맨앞의 메뉴?
                if (menu == 0)
                {
                    selectMenu = (((int)EIdxMENU.Count - 1) << SHIFT_MENU); //CONTENT 초기화
                    break;
                }

                selectMenu -= (1 << SHIFT_MENU);
                selectMenu &= ~MASK_CONTENT;   //MENU 변경 → CONTENT 초기화

                break;
            case IDxINPUT.DOWN:

                if ((selectMenu & MASK_CONTENT) == lastSlotIndex)
                    selectMenu &= ~MASK_CONTENT; //CONTENT 초기화
                else
                    selectMenu += 0x01;

                break;
            case IDxINPUT.UP:
                if ((selectMenu & MASK_CONTENT) == 0x00)
                    selectMenu |= lastSlotIndex;
                else
                    selectMenu -= 0x01;
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
    private void ProcTargeting()
    {
        //처리
        InputMgr.SetMode(IDxINPUT.BATTLE_TARGERT);  //입력모드: Targeting으로 설정
        Input_Targeting(0);

        //출력(표시)
        //Arrow는 여기에 속하지 않는다는게 이상한데?
        //으아ㅏㅏㅏㅏ
        //UI처리 단으로도 고민이 필요하겠군.
        Show(false);
    }
    public void Input_Targeting(int input)
    {
        //입력 포맷은 [0b_0111_1111] 형태이다.

        //input => Get Data
        SkillData skill = UnitMgr.Battle_GetSkill(nowOrder, menu, content);
        int min = (skill.TargetGroupType == 1) ? 3 : 0;
        int max = (skill.TargetGroupType == 1) ? 6 : 2;

        min = 1 << min;
        max = 1 << max;

        //input & data => select_exceptional
        if (selectTarget == 0 
            || skill.TargetGroupType == (int)ETargetGroup.Self
            || skill.TargetCountType == (int)ETargetCount.All)
        {
            //기본으로 세팅된 입력값(Self, All은 계산 방법이 항상 동일하다.)
            TargetingInput_Default(skill.TargetGroupType, skill.TargetCountType, FlagToIndex(min), FlagToIndex(max));

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
            case IDxINPUT.UP:
                {
                    while (true)
                    {
                        if (selectTarget == min)
                            selectTarget = max;
                        else
                            selectTarget >>= 1;

                        int idxPos = FlagToIndex(flag: selectTarget);
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
                        if (selectTarget == max)
                            selectTarget = min;
                        else
                            selectTarget <<= 1;

                        int idxPos = FlagToIndex(flag: selectTarget);
                        target = UnitMgr.Battle_GetUnit(idxPos);
                        if (target != null && !target.IsFaint)
                            break;
                    }
                }
                break;
        }

        Debug.Log($"[END] {skill.Name} => IDxPos[{Mathf.Log(selectTarget, 2)}]({nowOrder},{menu},{content})");

        //select => ui
        //TargetingUI_Update(skill);
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
    private void TargetingInput_Default(int targetGroup, int targetCount, int min, int max)
    {
        if (targetCount == (int)ETargetCount.All)
        {
            switch ((ETargetGroup)targetGroup)
            {
                case ETargetGroup.Party:        selectTarget = (0b_0000_0111);      return;
                case ETargetGroup.Enemy:        selectTarget = (0b_0111_1000);      return;
                case ETargetGroup.ExceptSelf:   selectTarget = ~(1 << nowOrder);    return;
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
                                selectTarget = (1 << i);
                                return;
                            }
                        }
                    }
                    return;
                case ETargetGroup.Self:
                    {
                        selectTarget = (1 << nowOrder);
                    }
                    return;
            }
        }
    }
    private void TargetingUI_Update()
    { 
        //네이밍 규칙? 처럼 보려고 일단 팠음
    }

    private void ProcUI_ChangeMode()
    {

    }
    private void ProcUI_UseItem()
    {

    }
}