using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Player;
using static UnityEditor.Progress;

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
    private GameObject slotPrefab;
    private Transform contentScroll;
    private List<UIBattleSlot> slots;

    private TextMeshProUGUI[] contentInfoText;

    private Transform[] targetingArrow;
    #endregion
    #region BitMask
    //Mask
    private const ushort playerMask     = 0b_1100_0000_0000_0000;   // << 14
    private const ushort groupMask      = 0b_0011_1000_0000_0000;   // << 11 
    private const ushort targetMask     = 0b_0000_0110_0000_0000;   // << 9
    private const ushort menuMask       = 0b_0000_0001_1100_0000;   // << 6
    private const ushort contentMask    = 0b_0000_0000_0011_1111;

    //Shift
    public const ushort ActorShift     = 14;    //현재 액션을 선택하는 유닛 인덱스(Players)
    public const ushort GroupShift     = 11;    //스킬의 타겟 그룹
    public const ushort TargetShift    = 9;     //스킬의 타겟 유닛
    public const ushort MenuShift      = 6;     //UI 메뉴
    #endregion

    //Index
    private static int select = UIIndex.BATTLE_ATK_BASIC;
    private static int contentMaxIndex;
    private static byte actorIndex;
    private static int menuIndex { get => (select >> MenuShift) - 1; }
    private static int contentIndex { get => select & contentMask; }

    //Now unit
    private static Unit selectedUnit { get => GameMgr.NowUnit; }

    public static void Show(bool on)
    {
        if (Instance == null)
        {
            GameObject go = Resources.Load<GameObject>("Prefab/UIBattle");
            go = Instantiate(go, UIMgr.UICanvas.transform);
            Instance = go.GetComponent<UIBattle>();

            //유저 턴? 선택 UI 오픈
            Unit actor = GameMgr.GetUnitByOrder(actorIndex);
            if (actor.Data.Group == UnitMgr.GROUP_PLY)
            {
                Instance.playerMenuPanel.SetActive(true);
                Instance.UpdateUIContent(actor, UIIndex.BATTLE_ATK_BASIC);
            }
            else
                Instance.playerMenuPanel.SetActive(false);
        }

        actorIndex = 0;
        Instance.gameObject.SetActive(on);
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
        slotPrefab = Resources.Load<GameObject>("Prefab/UIBattleSkill");
        contentScroll = content.GetChild(2).GetChild(0).GetChild(0);
        for (int i = 0; i < contentScroll.childCount; ++i)
            slots.Add(new UIBattleSlot(contentScroll.GetChild(i).gameObject));

        contentInfoText = content.GetChild(1).GetComponentsInChildren<TextMeshProUGUI>();
        targetingArrow = transform.GetChild(0).GetChild(2).GetComponentsInChildren<Transform>(true);
    }

    //Select Menu
    public static void SelectMenu(InputKey input)
    {
        //Get Input : Direction
        switch (input & InputKey.Direction)
        {
            case InputKey.Left:
                {
                    int check = select;
                    check -= (1 << MenuShift);
                    if ((check & menuMask) == UIIndex.BATTLE_MIN)
                        select = UIIndex.BATTLE_MAX - (1 << MenuShift);
                    else
                        select = check;

                    select &= ~contentMask;

                    Instance.UpdateUIContent(selectedUnit, select);
                }
                break;
            case InputKey.Right:
                {
                    int check = select;
                    check += (1 << MenuShift);
                    if ((check & menuMask) == UIIndex.BATTLE_MAX)
                        select = UIIndex.BATTLE_MIN + (1 << MenuShift);
                    else
                        select = check;

                    select &= ~contentMask;
                    Instance.UpdateUIContent(selectedUnit, select);
                }
                break;
            case InputKey.Up:
                {
                    if ((select & contentMask) == 0x00)
                        select |= contentMaxIndex;
                    else
                        select -= 0x01;
                }
                break;
            case InputKey.Down:
                {
                    if ((select & contentMask) == contentMaxIndex)
                        select &= ~contentMask;
                    else
                        select += 0x01;
                }
                break;
        }

        //Update UI (Arrow)
        Instance.contentArrow.anchoredPosition = contentArrowDefault + contentIndex * new Vector2(0, deltaContent);
        Instance.menuArrow.anchoredPosition = menuArrowDefault + menuIndex * new Vector2(deltaMenu, 0);

        //Get Input : Interact
        switch (input & InputKey.Interact)
        {
            case InputKey.Confirm:
                {
                    switch (select & menuMask) //메뉴 인덱스 비교
                    {
                        //Basic, Solo, Group, Special
                        default:
                            {
                                //Get Input : Select Skill
                                SkillData skill = UnitMgr.GetSkill(UnitMgr.MyPC, menuIndex, contentIndex);
                                select |= (actorIndex << ActorShift);           //Set Actor Index
                                select |= (skill.TargetGroup << GroupShift);    //Set TargetGroup Index

                                //그룹에 따라 호출하는 UI가 다른 셈인데...
                                //(1) 화면에 띄운다가 포인트인건가?
                                //(2) 

                                /*
                                0: none
                                1: 상대 개인
                                2: 본인 개인
                                3: 동료 개인

                                4: 상대 전체
                                5: 동료 전체

                                6: 본인 외 전체
                                7: 전체
                                */
                            }
                            break;
                        case UIIndex.BATTLE_CHANGE_MODE:
                            {
                                Debug.Log($"Change Mode");
                            }
                            break;
                        case UIIndex.BATTLE_USE_ITEM:
                            {
                                Debug.Log($"Use Item");
                            }
                            break;
                    }
                }
                break;
            case InputKey.Cancel:
                //스킬 대상 지정하기 전에 취소 누르면 다시 스킬 선택으로
                break;
            case InputKey.Info:
                //UIMain에 띄우던가 그래야 하네
                break;
        }
    }
    private void UpdateUIContent(Unit unit, int select)
    {
        //인덱스로 맞추기
        select >>= MenuShift;

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
        switch (select)
        {
            case 1: //기본기
            case 2: //개인기
            case 3: //단체기
            case 6: //특수행동
                {
                    SkillData[] skills = UnitMgr.GetSkillTypeof(unit, select).ToArray();
                    contentMaxIndex = skills.Length - 1;

                    //슬롯 추가 생성
                    AddSlot(skills.Length - slots.Count);

                    //슬롯 활성화 + 정보 입력
                    for (int i = 0; i < slots.Count; ++i)
                    {
                        if (i < skills.Length)
                            slots[i].Load(skills[i].Name, skills[i].RcsCode);
                        else
                            slots[i].SetActive(false);
                    }
                }
                break;
            case 4: //모드
                {
                    contentMaxIndex = Define.BattleMode.Length - 1;

                   //슬롯 추가 생성
                    AddSlot(Define.BattleMode.Length - slots.Count);

                    //메뉴 슬롯 (비)활성화
                    for (int i = 0; i < slots.Count; ++i)
                    {
                        if (i > contentMaxIndex)
                            slots[i].SetActive(false);
                        else
                            slots[i].Load(Define.BattleMode[i], "Icon_Mode");
                    }
                }
                break;
            case 5: //아이템
                {
                    //아이템이 없는 경우를 상정하질 않았네?
                    //이거 설계 대충했더니 결국 발목 잡히는군 ㅇㅋ..

                    Player.Item[] items = Items.ToArray();
                    contentMaxIndex = items.Length - 1;

                    //슬롯 추가 생성
                    AddSlot(items.Length - slots.Count);

                    //슬롯 활성화 + 정보 입력
                    for (int i = 0; i < slots.Count; ++i)
                    {
                        if (i < items.Length)
                            slots[i].Load(items[i].Tbl.Name, items[i].Tbl.RcsCode);
                        else
                            slots[i].SetActive(false);
                    }
                }
                break;
        }
    }


    //Select Target
    public static void SelectTarget(InputKey input)
    {
        int group = select >> GroupShift;


        //targetMaxIndex 설정
        int targetMaxIndex = -1;
        switch (group)
        {
            case 1: targetMaxIndex = UnitMgr.GetGroupCount(UnitMgr.GROUP_ENM); break;     //상대 개인
            case 3: targetMaxIndex = UnitMgr.GetGroupCount(UnitMgr.GROUP_PLY); break;     //동료 개인
        }

        //targeting
        if (targetMaxIndex == -1)
        {
            int check = select;
            switch (input & InputKey.Direction)
            {
                case InputKey.Up:
                case InputKey.Left:
                    {
                        check -= (1 << TargetShift); //언더플로가 발생할 수 있으니 따로 빼서 체크
                        if ((check & targetMask) >= (targetMaxIndex << TargetShift))
                            select &= (targetMaxIndex << TargetShift);
                        else
                            select = check;
                    }
                    break;
                case InputKey.Down:
                case InputKey.Right:
                    {
                        check += (1 << TargetShift);
                        if ((check & targetMask) == 0)
                            select &= ~targetMask;
                        else
                            select = check;
                    }
                    break;
            }
        }
        /*
        0: 상대 / 개인
        1: 본인 / 개인
        2: 동료 / 개인

        3: 상대 / 전체
        4: 동료 / 전체

        5: 본인 외 전체
        */
        
        //Transform uiElementTransform = uiElement.GetComponent<Transform>();        // RectTransform을 포함한 UI 요소의 Transform을 얻어옵니다.
        //RectTransform rectTransform = uiElement.GetComponent<RectTransform>();        // RectTransform을 얻어옵니다.

        // RectTransform의 위치를 UI 요소의 Transform에 맞춥니다.
        //Vector2 anchoredPosition = new Vector2(uiElementTransform.localPosition.x, uiElementTransform.localPosition.y);
        //rectTransform.anchoredPosition = anchoredPosition;

        //UpdateUnitTargeting(pos);
    }


    //Add Slot
    private void AddSlot(int count)
    {
        if (count <= 0)
            return;

        for (int i = 0; i < count; ++i)
        {
            GameObject slot = Instantiate(slotPrefab, contentScroll);
            slots.Add(new UIBattleSlot(slot));
        }
    }
}