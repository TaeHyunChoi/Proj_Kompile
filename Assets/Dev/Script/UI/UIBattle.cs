using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIBattle : MonoBehaviour
{
    public static UIBattle Instance { get; private set; }
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
    private const ushort shiftTarget    = 4 * 3;
    private const ushort shiftMenu      = 4 * 2;
    //private const ushrot shiftContent = 4 * 0;

    private const ushort maskTarget     = 0x7000;
    private const ushort maskMenu       = 0x0F00;
    private const ushort maskContent    = 0x00FF;
    #endregion
    #region Index
    private static string[] MODE = new string[] { "보통", "돌격", "방어", "선제", "반격" };

    private const byte menuMin          = 0;
    private const byte menuBasic        = 1;
    private const byte menuSkillSolo    = 2;
    private const byte menuSkillGroup   = 3; 
    private const byte menuMode         = 4;
    private const byte menuItem         = 5;
    private const byte menuSkillSpecial = 6;
    private const byte menuMax          = 7;

    private static int select = menuBasic;
    private static int selectMenu { get => (select & maskMenu) >> shiftMenu; }
    private static int selectContent { get => (select & maskContent); }
    private static int contentMax;

    #endregion

    private static int nowOrder { get => GameMgr.NowOrder; }

    public static void Init()
    {
        if (Instance != null)
            return;

        GameObject go = Resources.Load<GameObject>("Prefab/UIBattle");
        go = Instantiate(go, UIMgr.Canvas_Battle.transform);
        Instance = go.GetComponent<UIBattle>();
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
        Unit actor = UnitMgr.Battle_GetUnit(nowOrder);
        select = actor.LastAction;
        if (select <= 0)
            select = (menuBasic << shiftMenu);

        Instance.UpdateUIMenu();
        Instance.gameObject.SetActive(on);
        InputMgr.Set(IDxINPUT.BATTLE_MENU);
    }


    //Select Menu
    public static void SelectMenu(int input)
    {
        //Get Input : Interact (=> return;)
        switch (input & IDxINPUT.INTERACT)
        {
            case IDxINPUT.ENTER:
                {
                    switch (selectMenu)
                    {
                        //Get Input : Select Skill
                        default:
                            {
                                SkillData skill = UnitMgr.Battle_GetSkill(nowOrder, selectMenu, selectContent);
                                Debug.Log($"[{skill.Name}] {skill.TargetGroup}");

                                //전투를 어떻게 진행시킬 것인가?에 대한 설계 부족...
                                //단순 턴제 RPG인데 이렇게 어려울 일인가 ㅎㅎ...
                                //일단 타겟팅 : 단일부터 해보자...
                                //아 근데 졸림;

                                select ^= select;
                                InputMgr.Set(IDxINPUT.BATTLE_TARGERT);
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
                    return;
                }
            case IDxINPUT.CANCEL:
                //스킬 대상 지정하기 전에 취소 누르면 다시 스킬 선택으로
                return;
            case IDxINPUT.INFO:
                //UIMain에 띄우던가 그래야 하네
                return;
        }

        //Get Input : Direction
        switch (input & IDxINPUT.DIRECTION)
        {
            case IDxINPUT.LEFT:
                {
                    if ((selectMenu - 1) <= menuMin)
                        select = (menuMax - 1) << shiftMenu;
                    else
                        select -= (1 << shiftMenu);

                    Instance.UpdateUIMenu();
                }
                break;
            case IDxINPUT.RIGHT:
                {
                    if ((selectMenu + 1) >= menuMax)
                        select = (menuMin + 1) << shiftMenu;
                    else
                        select += (1 << shiftMenu);

                    Instance.UpdateUIMenu();
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
            case IDxINPUT.DOWN:
                {
                    if ((select & maskContent) == contentMax)
                        select &= ~maskContent;
                    else
                        select += 0x01;
                }
                break;
        }

        //Update UI : Direction
        Instance.contentArrow.anchoredPosition  = contentArrowDefault + selectContent * new Vector2(0, deltaContent);
        Instance.menuArrow.anchoredPosition     = menuArrowDefault + (selectMenu - 1) * new Vector2(deltaMenu, 0);
    }
    private void UpdateUIMenu()
    {
        //Update Title
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

        //Update Content
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

        //Update Content Max Index
        contentMax = count - 1;
    }


    //Select Target
    public static void SelectTarget(int input)
    {
        //Select Target에 대한 설계가 전혀 이뤄지지 않았군!

        //target_ENM_Solo
        //target_Self
        //target_PLY_Solo
        //target_ENM_All
        //target_PLY_All
        //target_SELF_XOR

        //targetMaxIndex 설정
        int targetMaxIndex = -1;
        //switch (select >> shiftGroup)
        //{

        //}

        //targeting
        if (targetMaxIndex == -1)
        {
            /*
            int check = select;
            switch (input & IDxINPUT.DIRECTION)
            {
                case IDxINPUT.UP:
                case IDxINPUT.LEFT:
                    {
                        check -= (1 << shiftTarget); //언더플로가 발생할 수 있으니 따로 빼서 체크
                        if ((check & maskTarget) >= (targetMaxIndex << shiftTarget))
                            select &= (targetMaxIndex << shiftTarget);
                        else
                            select = check;
                    }
                    break;
                case IDxINPUT.DOWN:
                case IDxINPUT.RIGHT:
                    {
                        check += (1 << shiftTarget);
                        if ((check & maskTarget) == 0)
                            select &= ~maskTarget;
                        else
                            select = check;
                    }
                    break;
            }
            //*/
        }

        //Transform uiElementTransform = uiElement.GetComponent<Transform>();        // RectTransform을 포함한 UI 요소의 Transform을 얻어옵니다.
        //RectTransform rectTransform = uiElement.GetComponent<RectTransform>();        // RectTransform을 얻어옵니다.

        // RectTransform의 위치를 UI 요소의 Transform에 맞춥니다.
        //Vector2 anchoredPosition = new Vector2(uiElementTransform.localPosition.x, uiElementTransform.localPosition.y);
        //rectTransform.anchoredPosition = anchoredPosition;

        //UpdateUnitTargeting(pos);
    }
    public void UpdateUITargeting()
    { 
        
    }
}