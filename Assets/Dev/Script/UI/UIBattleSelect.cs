using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIBattleSelect : MonoBehaviour
{
    public  static UIBattleSelect Instance { get => instance; }
    private static UIBattleSelect instance;
    public struct UIBattleSlot
    {
        private GameObject go;
        private Image icon;
        private TextMeshProUGUI name;

        public UIBattleSlot(GameObject _go)
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
    private RectTransform menuArrow;
    private RectTransform contentArrow;
    private static Vector2 menuArrowDefault;
    private static Vector2 contentArrowDefault;
    private static float deltaMenu = 150f;
    private static float deltaContent = -125f;

    private GameObject playerMenuPanel;
    private Transform contentScroll;
    private List<UIBattleSlot> slots;

    private TextMeshProUGUI[] contentInfoText;

    private Transform[] targetingArrow;
    #endregion
    #region BitMask
    private const ushort shiftTargetGroup = 4 * 4;
    private const ushort shiftTargetOne = 4 * 3;
    private const ushort shiftMenu = 4 * 2;
    //private const ushrot shiftContent     = 4 * 0;

    private const int maskTargetGroup = 0x000F_0000;
    private const int maskTargetOne = 0x0000_F000;
    private const int maskMenu = 0x0000_0F00;
    private const int maskContent = 0x0000_00FF;
    #endregion
    #region Index
    private static string[] MODE = new string[] { "보통", "돌격", "방어", "선제", "반격" };

    private const byte menuMin = 0;
    private const byte menuBasic = 1;
    private const byte menuSkillSolo = 2;
    private const byte menuSkillGroup = 3;
    private const byte menuMode = 4;
    private const byte menuItem = 5;
    private const byte menuSkillSpecial = 6;
    private const byte menuMax = 7;

    private static int select = menuBasic;
    private static int contentMax;
    private static int indexENMMax;

    private static int selectTargetGroup { get => (select & maskTargetGroup) >> shiftTargetGroup; }
    private static int selectTargetOne { get => (select & maskTargetOne) >> shiftTargetOne; }
    private static int selectMenu { get => (select & maskMenu) >> shiftMenu; }
    private static int selectContent { get => (select & maskContent); }
    #endregion

    private static int nowOrder { get => GameMgr.NowOrder; }

    public static void Init()
    {
        if (Instance != null)
            return;

        GameObject go = Resources.Load<GameObject>("Prefab/UIBattleMenu");
        go = Instantiate(go, UIMgr.Canvas_Battle.transform);
        instance = go.GetComponent<UIBattleSelect>();
        Instance.gameObject.SetActive(false);
    }
    private void Awake()
    {
        playerMenuPanel = transform.GetChild(0).gameObject;

        Transform menu = transform.GetChild(0).GetChild(0);
        menuArrow = menu.GetChild(3).GetComponent<RectTransform>();
        menuArrowDefault = menuArrow.anchoredPosition;

        Transform content = transform.GetChild(0).GetChild(1);
        contentArrow = content.GetChild(3).GetComponent<RectTransform>();
        contentArrowDefault = contentArrow.anchoredPosition;

        slots = new List<UIBattleSlot>();
        contentScroll = content.GetChild(2).GetChild(0).GetChild(0);
        for (int i = 0; i < contentScroll.childCount; ++i)
            slots.Add(new UIBattleSlot(contentScroll.GetChild(i).gameObject));

        contentInfoText = content.GetChild(1).GetComponentsInChildren<TextMeshProUGUI>();
        targetingArrow = transform.GetChild(0).GetChild(2).GetComponentsInChildren<Transform>(true);
    }

    public static void Show(bool on)
    {
        Instance.gameObject.SetActive(on);
        if (!on)
            return;

        //Get Unit`s Last Select(Act)
        Unit actor = UnitMgr.Battle_GetUnit(nowOrder);
        select = (actor.LastSelect > 0) ? actor.LastSelect : (menuBasic << shiftMenu);
        
        Instance.UpdateUI(init: true);
        InputMgr.SetMode(IDxINPUT.BATTLE_MENU);
    }

    //얘가 좀 짜치네?
    public static void Set_TargetMaxCount(int count)
    {
        indexENMMax = (count - 1);
    }


    public static void Select_Menu(int input)
    {
        switch (input & IDxINPUT.INTERACT)
        {
            case IDxINPUT.ENTER:
                {
                    switch (selectMenu)
                    {
                        default: //Select Skill
                            {
                                int last = UnitMgr.Battle_GetLastAction(nowOrder);
                                last = (last & maskTargetOne);
                                select &= ~maskTargetOne;
                                select |= last;

                                SkillData skill = UnitMgr.Battle_GetSkill(nowOrder, selectMenu, selectContent);
                                select |= (skill.TargetGroup) << shiftTargetGroup;

                                UnitMgr.Battle_SetTarget(selectTargetGroup, selectTargetOne);
                                InputMgr.SetMode(IDxINPUT.BATTLE_TARGERT);
                            }
                            break;
                        case menuMode:
                            {
                                Debug.Log($"Change Mode");
                            }
                            break;
                        case menuItem:
                            {
                                Debug.Log($"Use Item");
                            }
                            break;
                    }

                    UnitMgr.Battle_SaveUnitAction(nowOrder, select);
                }
                return;
        }
        switch (input & IDxINPUT.DIRECTION)
        {
            case IDxINPUT.RIGHT:
                {
                    if ((selectMenu + 1) >= menuMax)
                        select = (menuMin + 1) << shiftMenu;
                    else
                        select += (1 << shiftMenu);

                    select &= ~maskContent;
                }
                break;
            case IDxINPUT.LEFT:
                {
                    if ((selectMenu - 1) <= menuMin)
                        select = (menuMax - 1) << shiftMenu;
                    else
                        select -= (1 << shiftMenu);

                    select &= ~maskContent;
                }
                break;
            case IDxINPUT.DOWN:
                {
                    if ((select & maskContent) == contentMax)
                        select &= ~maskContent;
                    else
                        select += 0x01;
                }
                break;
            case IDxINPUT.UP:
                {
                    if ((select & maskContent) == 0x00)
                        select |= contentMax;
                    else
                        select -= 0x01;
                }
                break;
        }
        Instance.UpdateUI();
    }
    public static void Select_Target(int input)
    {
        SkillData skill = UnitMgr.Battle_GetSkill(nowOrder, selectMenu, selectContent);
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
    }
    private static void Reset_Target()
    {
        UnitMgr.Battle_ResetTarget(selectTargetGroup, selectTargetOne);
        select &= ~maskTargetGroup;
        select &= ~maskTargetOne;
    }


    private void UpdateUI(bool init = false)
    {
        //Update Arrow (must)
        contentArrow.anchoredPosition = contentArrowDefault + selectContent * new Vector2(0, deltaContent);
        menuArrow.anchoredPosition = menuArrowDefault + (selectMenu - 1) * new Vector2(deltaMenu, 0);

        //Update Window Panel (conditional)
        if (!init && selectContent != 0)
            return;

        //Window Title
        switch (selectMenu)
        {
            case 1:
                contentInfoText[0].text = "기본기";
                contentInfoText[1].text = string.Empty;
                break;
            case 2:
                contentInfoText[0].text = "개인 공격기";
                contentInfoText[1].text = "MP";
                break;
            case 3:
                contentInfoText[0].text = "전체 공격기";
                contentInfoText[1].text = "MP";
                break;
            case 4:
                contentInfoText[0].text = "모드";
                contentInfoText[1].text = string.Empty;
                break;
            case 5:
                contentInfoText[0].text = "아이템";
                contentInfoText[1].text = string.Empty;
                break;
            case 6:
                contentInfoText[0].text = "특수기";
                contentInfoText[1].text = string.Empty;
                break;
        }

        //Window Content
        int i = 0, count = 0;
        List<string>[] code = new List<string>[2];
        code[0] = new List<string>();
        code[1] = new List<string>();
        switch (selectMenu)
        {
            case menuBasic:
            case menuSkillSolo:
            case menuSkillGroup:
            case menuSkillSpecial:
                {
                    List<SkillData> skills = UnitMgr.Battle_GetSkillTypeof(nowOrder, selectMenu);
                    count = skills.Count;

                    for (i = 0; i < count; i++)
                    {
                        code[0].Add(skills[i].Name);
                        code[1].Add(skills[i].RcsCode);
                    }
                }
                break;
            case menuMode:
                {
                    count = MODE.Length;

                    for (i = 0; i < count; i++)
                    {
                        code[0].Add(MODE[i]);
                        code[1].Add("Icon_Mode"); //리소스가 없으요...
                    }
                }
                break;
            case menuItem:
                {
                    List<Player.Item> items = Player.Items;
                    count = items.Count;

                    for (i = 0; i < count; i++)
                    {
                        code[0].Add(items[i].Tbl.Name);
                        code[1].Add(items[i].Tbl.RcsCode);
                    }
                }
                break;
        }

        //Use Slot => New or Active(true)
        for (i = 0; i < count; ++i)
        {
            if (i >= slots.Count)
            {
                GameObject rcs = ResourceMgr.Prefab["UIBattleSkill"];
                GameObject slot = Instantiate(rcs, contentScroll);
                slots.Add(new UIBattleSlot(slot));
            }

            slots[i].Load(code[0][i], code[1][i]);
        }

        //Not Used Slot => Active(false)
        for (; i < slots.Count; ++i)
            slots[i].SetActive(false);

        //Update Window Content Max Index
        contentMax = count - 1;
    }
}