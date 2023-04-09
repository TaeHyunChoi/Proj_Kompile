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
    private const ushort shiftActor     = 14;    //현재 액션을 선택하는 유닛 인덱스
    private const ushort shiftGroup     = 11;    //스킬의 타겟 그룹
    private const ushort shiftTarget    = 9;     //스킬의 타겟 유닛
    private const ushort shiftMenu      = 6;     //UI 메뉴

    private const ushort maskActor      = 0b_11                     << shiftActor;
    private const ushort maskGroup      = 0b_0011_1                 << shiftGroup;
    private const ushort maskTarget     = 0b_0000_011               << shiftTarget;
    private const ushort maskMenu       = 0b_0000_0001_11           << shiftMenu;
    private const ushort maskContent    = 0b_0000_0000_0011_1111;
    #endregion
    #region Index
    private static string[] MODE = new string[] { "보통", "돌격", "방어", "선제", "반격" };

    private const byte modeNormal          = 0;    //보통
    private const byte modeCharge          = 1;    //돌격
    private const byte modeDefence         = 2;    //방어
    private const byte modePreeemptive     = 3;    //선제
    private const byte modeCounter         = 4;    //반격

    private const ushort idxMin             = 0;
    private const ushort idxAtkBasic        = IDxSkill.BASIC << shiftMenu;
    private const ushort idxSkillSolo       = IDxSkill.SOLO << shiftMenu;
    private const ushort idxSkillGroup      = IDxSkill.GROUP << shiftMenu; 
    private const ushort idxMode            = 4 << shiftMenu;
    private const ushort idxItem            = 5 << shiftMenu;
    private const ushort idxSkillSpecial    = IDxSkill.SPECIAL << shiftMenu;
    private const ushort idxMax             = 7 << shiftMenu;

    private const byte target_ENM_Solo = 0;
    private const byte target_SLF = 1;
    private const byte target_PLY_Solo = 2;
    private const byte target_ENM_All = 3;
    private const byte target_PLY_All = 4;
    private const byte target_SLF_XOR = 5;

    private static int select = idxAtkBasic;
    private static int idxMenu { get => select >> shiftMenu; }
    private static int idxContent { get => select & maskContent; }

    private static int contentMaxIndex;
    private static byte actorIndex;

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
        if (select < 0)
            select = (idxAtkBasic | 0x00);

        Instance.UpdateUIMenu(select);
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
                    //select와 action의 차이가 있나..? 용도 다름 + 패킹 포지션 다름 ㅈㅅ;

                    int action = select;
                    switch (select & maskMenu) //메뉴 인덱스 비교
                    {
                        //Get Input : Select Skill
                        default:
                            {
                                //스킬을 이런 식으로 가져와야 하나...?
                                //아이코 데이터 프레임 쪽에서 또 음청 꼬였구나 후
                                SkillData skill = UnitMgr.Battle_GetSkill(nowOrder, idxMenu, idxContent);
                                action |= (actorIndex << shiftActor);           //Set Actor Index
                                action |= (skill.TargetGroup << shiftGroup);    //Set TargetGroup Index

                                select ^= select;
                                InputMgr.Set(IDxINPUT.BATTLE_TARGERT);
                            }
                            break;
                        case idxMode:
                            {
                                Debug.Log($"Change Mode");
                            }
                            break;
                        case idxItem:
                            {
                                Debug.Log($"Use Item");
                            }
                            break;
                    }

                    UnitMgr.Battle_SaveUnitAction(nowOrder, action);
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
                    int check = select;
                    check -= (1 << shiftMenu);
                    if ((check & maskMenu) == idxMin)
                        select = idxMax - (1 << shiftMenu);
                    else
                        select = check;

                    select &= ~maskContent;
                    Instance.UpdateUIMenu(select);
                }
                break;
            case IDxINPUT.RIGHT:
                {
                    int check = select;
                    check += (1 << shiftMenu);
                    if ((check & maskMenu) == idxMax)
                        select = idxMin + (1 << shiftMenu);
                    else
                        select = check;

                    select &= ~maskContent;
                    Instance.UpdateUIMenu(select);
                }
                break;
            case IDxINPUT.UP:
                {
                    if ((select & maskContent) == 0x00)
                        select |= contentMaxIndex;
                    else
                        select -= 0x01;
                }
                break;
            case IDxINPUT.DOWN:
                {
                    if ((select & maskContent) == contentMaxIndex)
                        select &= ~maskContent;
                    else
                        select += 0x01;
                }
                break;
        }

        //Update UI : Direction
        Instance.contentArrow.anchoredPosition = contentArrowDefault + idxContent * new Vector2(0, deltaContent);
        Instance.menuArrow.anchoredPosition = menuArrowDefault + (idxMenu - 1) * new Vector2(deltaMenu, 0);
    }
    private void UpdateUIMenu(int select)
    {
        //인덱스로 맞추기
        select >>= shiftMenu;

        //Menu Title
        switch (select)
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

        //Content
        int i = 0, count = 0;
        List<string>[] code = new List<string>[2];
        code[0] = new List<string>();
        code[1] = new List<string>();
        switch (select)
        {
            case 1: //기본기
            case 2: //개인기
            case 3: //단체기
            case 6: //특수행동
                {
                    List<SkillData> skills = UnitMgr.Battle_GetSkillTypeof(nowOrder, select);
                    count = skills.Count;

                    for (i = 0; i < count; i++)
                    {
                        code[0].Add(skills[i].Name);
                        code[1].Add(skills[i].RcsCode);
                    }
                }
                break;
            case 4: //모드
                {
                    count = MODE.Length;

                    for (i = 0; i < count; i++)
                    {
                        code[0].Add(MODE[i]);
                        code[1].Add("Icon_Mode"); //리소스가 없으요...
                    }
                }
                break;
            case 5: //아이템
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

        //사용 슬롯 > 생성 또는 갱신
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

        //사용하지 않는 슬롯 > 비활성화
        for (; i < slots.Count; ++i)
            slots[i].SetActive(false);

        //인덱스 최대값 갱신
        contentMaxIndex = count - 1;
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