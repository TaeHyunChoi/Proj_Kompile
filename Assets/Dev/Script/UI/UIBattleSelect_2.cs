using Mono.Cecil.Cil;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIBattleSelect;

public class UIBattleSelect_2 : MonoBehaviour
{
    private static UIBattleSelect_2 instance;
    private enum EIdxMENU : int
    {
        SkillBasic = 0,
        SkillSolo,
        SkillGroup,
        SkillSpecial,
        Mode,
        Item
    }

    #region UI
    private RectTransform menuArrow;
    private RectTransform contentArrow;

    private static Vector2  menuArrowDefault;
    private static Vector2  contentArrowDefault;
    private static float    deltaMenu = 150f;
    private static float    deltaContent = -125f;

    private Transform contentScroll;
    private List<UISlot_Battle> slots;

    private TextMeshProUGUI[] contentTitleText;

    private Transform[] targetingArrow;
    #endregion
    #region BitMask
    private static int MASK_TARGET   = 0x000F_F000;
    private static int MASK_MENU     = 0x0000_0F00;
    private static int MASK_CONTENT  = 0x0000_00FF;

    private static int SHIFT_TARGET  = 4 * 3;
    private static int SHIFT_MENU    = 4 * 2;
    private static int SHIFT_CONTENT = 0;
    #endregion
    #region Index
    private int select;         //Total Input Value
    private int selectMenu      { get => (select & MASK_MENU) >> SHIFT_MENU; }
    private int selectContent   { get => (select & MASK_CONTENT) >> SHIFT_CONTENT; }
    private int selectTarget    { get => (select & MASK_TARGET) >> SHIFT_TARGET; }
    private int selectMax;  //선택 가능한 마지막 인덱스
    #endregion

    private int nowOrder { get => GameMgr.NowOrder; }


    //Instantiate => Show
    public static void Instantiate()
    {
        if (instance != null)
            return;

        GameObject go = Resources.Load<GameObject>("Prefab/UIBattleMenu");
        go = Instantiate(go, UIMgr.Canvas_Battle.transform);
        instance = go.GetComponent<UIBattleSelect_2>();
        //Awake() 호출
    }
    private void Awake()
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
        targetingArrow = transform.GetChild(0).GetChild(2).GetComponentsInChildren<Transform>(true);

        //instance.gameObject.SetActive(false);
        instance.Show(false);
    }
    public void Show(bool on)
    {
        gameObject.SetActive(on);
        if (!on)
            return;

        //[HOLD] Get Unit`s Last Select(Act)
        select = 0;

        menuArrow.anchoredPosition = menuArrowDefault + selectMenu * new Vector2(deltaMenu, 0);
        contentArrow.anchoredPosition = contentArrowDefault + selectContent * new Vector2(0, deltaContent);
        UpdateUI_Content();
        InputMgr.SetMode(IDxINPUT.BATTLE_MENU);
    }


    //Input => Update Content
    public void Input(int input)
    {
        switch (input & IDxINPUT.INTERACT)
        {
            case IDxINPUT.ENTER:
                {
                    switch ((EIdxMENU)selectMenu)
                    {
                        default: //Select Skill
                            {
                                //int last = UnitMgr.Battle_GetLastAction(nowOrder);
                                //last = (last & maskTargetOne);
                                //select &= ~maskTargetOne;
                                //select |= last;

                                //SkillData skill = UnitMgr.Battle_GetSkill(nowOrder, selectMenu, selectContent);
                                //select |= (skill.TargetGroup) << shiftTargetGroup;

                                //UnitMgr.Battle_SetTarget(selectTargetGroup, selectTargetOne);
                                //InputMgr.SetMode(IDxINPUT.BATTLE_TARGERT);
                            }
                            break;
                        case EIdxMENU.Mode:
                            {
                                Debug.Log($"Change Mode");
                            }
                            break;
                        case EIdxMENU.Item:
                            {
                                Debug.Log($"Use Item");
                            }
                            break;
                    }
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
                    if ((selectMenu + 1) >= selectMax)
                        select = 1 << SHIFT_MENU;
                    else
                        select += (1 << SHIFT_MENU);

                    //MENU를 바꾼거라 CONTENT 선택 다 날렸군?
                    //기존 <환세취호전> 보고 Arrow 어디에 남아있는지 확인?
                    select &= ~MASK_MENU; 
                }
                break;
            case IDxINPUT.LEFT:
                {
                    if ((selectMenu - 1) <= 0)
                        select = (selectMax - 1) << SHIFT_MENU;
                    else
                        select -= (1 << SHIFT_MENU);

                    //MENU를 바꾼거라 CONTENT 선택 다 날렸군?
                    select &= ~MASK_MENU;
                }
                break;
            case IDxINPUT.DOWN:
                {
                    if ((select & MASK_CONTENT) == selectMax)
                        select &= ~MASK_CONTENT;
                    else
                        select += 0x01;
                }
                break;
            case IDxINPUT.UP:
                {
                    if ((select & MASK_CONTENT) == 0x00)
                        select |= selectMax;
                    else
                        select -= 0x01;
                }
                break;
        }

        UpdateUI_Content();
    }
    private void UpdateUI_Content() //메뉴판 전체 교체
    {
        string[] text = new string[2];
        string[,] code = new string[,] { };
        int count = 0;

        switch ((EIdxMENU)selectMenu)
        {
            case EIdxMENU.SkillBasic:
                text[0] = "기본기";
                text[1] = string.Empty;
                code = GetSlotData_Skill(type: EIdxMENU.SkillBasic, out count);
                break;
            case EIdxMENU.SkillSolo:
                text[0] = "개인 공격기";
                text[1] = "MP";
                code = GetSlotData_Skill(type: EIdxMENU.SkillSolo, out count);
                break;
            case EIdxMENU.SkillGroup:
                text[0] = "전체 공격기";
                text[1] = "MP";
                code = GetSlotData_Skill(type: EIdxMENU.SkillGroup, out count);
                break;
            case EIdxMENU.Mode:
                text[0] = "모드";
                text[1] = string.Empty;
                code = GetSlotData_Mode(out count);
                break;
            case EIdxMENU.Item:
                text[0] = "아이템";
                text[1] = string.Empty;
                code = GetSlotData_Item(out count);
                break;
            case EIdxMENU.SkillSpecial:
                text[0] = "특수기";
                text[1] = string.Empty;
                code = GetSlotData_Skill(type: EIdxMENU.SkillSpecial, out count);
                break;
        }

        contentTitleText[0].text = text[0];
        contentTitleText[1].text = text[1];

        //Use Slot => New or Active(true)
        GameObject rcs = ResourceMgr.Prefab["UIBattleSkill"];
        for (int i = 0; i < count; ++i)
        {
            if (i >= slots.Count)
            {
                GameObject slot = Instantiate(rcs, contentScroll);
                slots.Add(new UISlot_Battle(slot));
            }

            slots[i].Load(code[0,i], code[1,i]);
        }

        //Not Used Slot => Active(false)
        for (int i = 0; i < slots.Count; ++i)
            slots[i].SetActive(false);

        //Update Window Content Max Index
        selectMax = count - 1;
    }
    private string[,] GetSlotData_Skill(EIdxMENU type, out int count)
    {
        List<SkillData> skills = UnitMgr.Battle_GetSkillTypeof(nowOrder, (int)type);
        string[,] code = new string[2, skills.Count];
        count = code.GetLength(1);

        for (int i = 0; i < code.GetLength(1); i++)
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

        for (int i = 0; i < code.GetLength(1); i++)
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

        for (int i = 0; i < code.GetLength(1); i++)
        {
            code[0, i] = items[i].Tbl.Name;
            code[1, i] = items[i].Tbl.RcsCode;
        }

        return code;
    }

    //Input => Update Target

}
