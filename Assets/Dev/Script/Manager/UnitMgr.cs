using System.Collections.Generic;
using UnityEngine;

public class UnitMgr
{
    private static List<Unit> unitAll    = new List<Unit>();
    
    private static List<Unit> unitInBattle = new List<Unit>();
    private static Queue<int> battleOrder = new Queue<int>();

    public static Unit NowActor { get => unitInBattle[GameMgr.NowOrder]; } //없어질 친구로군

    private static Transform tfActive;
    private static Transform tfInactive;

    public  static  Unit MyPC { get => myPC; }
    private static  Unit myPC;

    public static bool IsEndBattle()
    {
        if (unitInBattle.FindAll(unit => unit.Data.Group == IDxUNIT.PARTY).Count <= 0)
            return true;
        if (unitInBattle.FindAll(unit => unit.Data.Group == IDxUNIT.ENEMY).Count <= 0)
            return true;

        return false;
    }
    public static bool IsEndCycle(int order)
    {
        return order >= (unitInBattle.Count - 1);
    }


    public static void Init(Transform tf)
    {
        tfActive = tf.GetChild(0);
        tfInactive = tf.GetChild(1);
    }
    public static Unit New(int unitCode, Vector3 pos)
    {
        Unit newUnit;
        if (tfInactive.childCount > 0)
        {
            newUnit = tfInactive.GetChild(0).GetComponent<Unit>();
        }
        else
        {
            GameObject go = ResourceMgr.Prefab["UnitBase"];
            go = GameObject.Instantiate(go, tfActive);
            newUnit = go.GetComponent<Unit>();
        }

        newUnit.Init(unitCode);
        newUnit.transform.position = pos;
        newUnit.transform.eulerAngles = new Vector3(50, 0, 0);
        newUnit.gameObject.name = newUnit.Data.RcsCode;

        unitAll.Add(newUnit);
        return newUnit;
    }
    public static void Test_SetMyPC()
    {
        myPC = unitAll[IDxUNIT.ATAHO];
    }


    //## Battle > Set Battle Situation
    public static void ProcUnit_EnterBattle(MapData map, int order)
    {
        Battle_Init(map);               //전투에 참여하는 유닛 생성 & 배치
        Battle_SetOrder();              //전투 순서 결정
        Battle_SelectAction(order);     //[order]번째 유닛 액션 선택

        UIMgr.Battle_InitTargetingArrows(unitInBattle);  //타겟팅 표시 UI 배치
    }
    private static void Battle_Init(MapData map)
    {
        List<Unit> units;
        Vector3     standard;
        float       delta;

        //## Get Party Data => Add Battle List
        units = unitAll.FindAll(unit => unit.Data.Group == IDxUNIT.PARTY);
        unitInBattle.AddRange(units);

        //## Set Party Position
        standard = new Vector3(-4f, 100f, -3.3f);
        switch (units.Count)
        {
            case 1:
                delta = -1.5f;
                units[0].transform.localPosition = standard + new Vector3(0, 0, delta);
                break;
            case 2:
                delta = -1f;
                units[0].transform.localPosition = standard + new Vector3(0, 0, delta);
                units[1].transform.localPosition = standard + new Vector3(0, 0, delta * 2);
                break;
            case 3:
                delta = -1.5f;
                units[0].transform.localPosition = standard;
                units[1].transform.localPosition = standard + new Vector3(0, 0, delta);
                units[2].transform.localPosition = standard + new Vector3(0, 0, delta * 2);
                break;
        }
        int start = units.Count;

        //## Set Enemy Data => Add Battle List
        units.Clear();
        int count = Random.Range(map.MinCount, map.MaxCount + 1); //맵 Mob 개수
        UIBattleSelect.Set_TargetMaxCount(count); //짜친다
        int variety = map.Mob.Length;

        byte index;
        for (int i = 0; i < count; ++i)
        {
            index = map.Mob[Random.Range(0, variety)];
            units.Add(New(index, Vector3.zero));
        }
        unitInBattle.AddRange(units);

        //## Set Enemy Position
        standard = new Vector3(4.5f, 100f, -3f);
        switch (units.Count)
        {
            case 1:
                delta = -1.325f;
                units[0].transform.localPosition = standard + new Vector3(0, 0, delta);
                break;
            case 2:
                delta = -1.25f;
                units[0].transform.localPosition = standard + new Vector3(0, 0, delta);
                units[1].transform.localPosition = standard + new Vector3(0, 0, delta * 2);
                break;
            case 3:
                delta = -1.25f;
                units[0].transform.localPosition = standard + new Vector3(-0.5f, 0, delta);
                units[1].transform.localPosition = standard + new Vector3(0, 0, delta * 2);
                units[2].transform.localPosition = standard + new Vector3(-0.5f, 0, delta * 3);
                break;
            case 4:
                delta = -1.25f;
                units[0].transform.localPosition = standard + new Vector3(-0.5f, 0, 0);
                units[1].transform.localPosition = standard + new Vector3(0, 0, delta);
                units[2].transform.localPosition = standard + new Vector3(0, 0, delta * 2);
                units[3].transform.localPosition = standard + new Vector3(-1f, 0, delta * 3);
                break;
        }

        //## Set Battle Unit`s Stats
        for (int i = 0; i < unitInBattle.Count; ++i)
            unitInBattle[i].Battle_SetStatus();
    }
    public  static void Battle_SetOrder()
    {
        int count = unitInBattle.Count;
        int[] arr = new int[count];
        for (int i = 0; i < count; ++i)
            arr[i] = i;

        OrderByQuick(ref arr, 0, count - 1);

        for (int i = 0; i < arr.Length; ++i)
        {
            battleOrder.Enqueue(arr[i]);
            Debug.Log($"Pos[{arr[i]}] {unitInBattle[arr[i]].Data.Name}.Prior: {unitInBattle[arr[i]].Priority:F2}");
        }
    }
    private static void OrderByQuick(ref int[] arr, int start, int end)
    {
        int idxLeft = start;
        int idxRight = end;
        float pivotPrior = unitInBattle[arr[(idxLeft + idxRight) >> 1]].Priority;

        //[내림차순] pivot 기준으로 왼쪽은 큰 수, 오른쪽은 작은 수로 정렬 (1 cycle)
        while (idxLeft < idxRight)
        {
            while (unitInBattle[arr[idxLeft]].Priority > pivotPrior)
                ++idxLeft;
            while (unitInBattle[arr[idxRight]].Priority < pivotPrior)
                --idxRight;

            if (idxLeft > idxRight)
                break;

            if (unitInBattle[arr[idxLeft]].Priority != unitInBattle[arr[idxRight]].Priority)
            {
                arr[idxLeft] ^= arr[idxRight];
                arr[idxRight] ^= arr[idxLeft];
                arr[idxLeft] ^= arr[idxRight];
            }

            ++idxLeft;
            --idxRight;
        }

        //left, right 위치 역전됨 → 가독성을 위해 Swap(left, right)
        if (idxLeft != idxRight)
        {
            idxLeft ^= idxRight;
            idxRight ^= idxLeft;
            idxLeft ^= idxRight;
        }

        if (start < idxLeft)
            OrderByQuick(ref arr, start, idxLeft);
        if (idxRight < end)
            OrderByQuick(ref arr, idxRight, end);
    }

    //## Battle > Unit Data (for Action)
    public static Unit Battle_GetUnit(int order)
    {
        return unitInBattle[order];
    }
    public static Unit Battle_GetUnit(int group, int order)
    {
        Unit[] targetGroup = unitInBattle.FindAll(unit => unit.Data.Group == group).ToArray();
        return targetGroup[order];
    }
    public static List<Unit> Battle_GetUnitGroup(int group)
    {
        return unitInBattle.FindAll(unit => unit.Data.Group == group);
    }
    public static List<SkillData> Battle_GetSkillTypeof(int order, int type)
    {
        return unitInBattle[order].Skill[type];
    }
    public static SkillData Battle_GetSkill(int order, int type, int index)
    {
        return unitInBattle[order].Skill[type][index];
    }
    public static void Battle_SaveUnitAction(int order, int act)
    {
        unitInBattle[order].Battle_SaveLastAction(act);
    }


    //## Battle > UI
    public static void Battle_SetTarget(int group, int targetIndex)
    {
        switch (group)
        {
            case IDxUNIT.TARGET_ENM_SOLO:
            case IDxUNIT.TARGET_PRT_SOLO:
                {
                    //TargetGroup index, Group index 서로 다름
                    group = (group == IDxUNIT.TARGET_ENM_SOLO) ? IDxUNIT.ENEMY : IDxUNIT.PARTY;
                    List<Unit> groupUnits = unitInBattle.FindAll(unit => unit.Data.Group == group);
                    for (int i = 0; i < groupUnits.Count; ++i)
                        groupUnits[i].Battle_BeTargeted(i == targetIndex);
                    break;
                }
            case IDxUNIT.TARGET_ENM_ALL:
            case IDxUNIT.TARGET_PRT_ALL:
                {
                    group = (group == IDxUNIT.TARGET_ENM_ALL) ? IDxUNIT.ENEMY : IDxUNIT.PARTY;
                    List<Unit> groupUnits = unitInBattle.FindAll(unit => unit.Data.Group == group);
                    for (int i = 0; i < groupUnits.Count; ++i)
                        groupUnits[i].Battle_BeTargeted(true);
                    break;
                }
            case IDxUNIT.TARGET_SELF:
                {
                    unitInBattle[GameMgr.NowOrder].Battle_BeTargeted(true);
                    break;
                }
            case IDxUNIT.TARGET_XOR_SELF:
                {
                    for (int i = 0; i < unitInBattle.Count; ++i)
                        unitInBattle[i].Battle_BeTargeted(i != GameMgr.NowOrder);
                    break;
                }
        }
    }
    public static void Battle_ResetTarget(int group, int targetIndex)
    {
        switch (group)
        {
            case IDxUNIT.TARGET_ENM_SOLO:
            case IDxUNIT.TARGET_PRT_SOLO:
                {
                    group = (group == IDxUNIT.TARGET_ENM_SOLO) ? IDxUNIT.ENEMY : IDxUNIT.PARTY;
                    List<Unit> groupUnits = unitInBattle.FindAll(unit => unit.Data.Group == group);
                    groupUnits[targetIndex].Battle_BeTargeted(false);
                    break;
                }
            case IDxUNIT.TARGET_ENM_ALL:
            case IDxUNIT.TARGET_PRT_ALL:
                {
                    group = (group == IDxUNIT.TARGET_ENM_ALL) ? IDxUNIT.ENEMY : IDxUNIT.PARTY;
                    List<Unit> groupUnits = unitInBattle.FindAll(unit => unit.Data.Group == group);
                    for (int i = 0; i < groupUnits.Count; ++i)
                        groupUnits[i].Battle_BeTargeted(false);
                    break;
                }
            case IDxUNIT.TARGET_SELF:
                {
                    unitInBattle[GameMgr.NowOrder].Battle_BeTargeted(false);
                    break;
                }
            case IDxUNIT.TARGET_XOR_SELF:
                {
                    //하나 정도는 걍 중복 처리하자...
                    for (int i = 0; i < unitInBattle.Count; ++i)
                        unitInBattle[i].Battle_BeTargeted(false);
                    break;
                }
        }
    }
    public static int  Battle_GetLastAction(int order)
    {
        return unitInBattle[order].LastSelect;
    }


    //## Battle > Action
    public static void Battle_SelectAction(int order)
    {
        if (unitInBattle[order].Data.Group == IDxUNIT.PARTY)
            UIMgr.Show(IDxUI.BATTLE_SELECT, true);
        else
            unitInBattle[order].Battle_AI();
    }
    public static void Battle_ActUnit(int order, SkillData skill, int select)
    {
        List<Unit> targets = new List<Unit>();

        //더 좋은 수가 있나?
        int group = (select & 0x000F_0000) >> 16;           //UIBattle.selectTargetGroup
        int soloTarget  = (select & 0x0000_F000) >> 12;     //UIBattle.selectTargetOne

        switch (group)
        {
            case IDxUNIT.TARGET_ENM_SOLO:
            case IDxUNIT.TARGET_PRT_SOLO:
                group = (group == IDxUNIT.TARGET_ENM_SOLO) ? IDxUNIT.ENEMY : IDxUNIT.PARTY;
                targets.Add(Battle_GetUnit(group, soloTarget));
                break;
            case IDxUNIT.TARGET_ENM_ALL:
            case IDxUNIT.TARGET_PRT_ALL:
                group = (group == IDxUNIT.TARGET_ENM_ALL) ? IDxUNIT.ENEMY : IDxUNIT.PARTY;
                targets.AddRange(Battle_GetUnitGroup(group));
                break;
            case IDxUNIT.TARGET_SELF:
                targets.Add(unitInBattle[order]);
                break;
            case IDxUNIT.TARGET_XOR_SELF:
                targets.AddRange(Battle_GetUnitGroup(IDxUNIT.TARGET_ENM_ALL));
                targets.AddRange(Battle_GetUnitGroup(IDxUNIT.TARGET_PRT_ALL));
                targets.Remove(unitInBattle[order]);
                break;
        }

        unitInBattle[order].Battle_SaveLastAction(select);
        unitInBattle[order].Battle_PlayAction(skill, targets);
    }
    public static void Battle_SetRenderOrder(bool isTurn)
    {
        int order = isTurn ? 2 : 0;
        NowActor.SetRenderOrder(order);

        List<Unit> targets = NowActor.Targets;
        for (int i = 0; i < targets.Count; ++i)
            targets[i].SetRenderOrder(order);
    }
    public static void Battle_SlowUnitAnime(bool slow, float lerpWeight = 1f)
    {
        float end = slow ? 0.1f : 1;

        NowActor.SetAnimeSpeed(end, lerpWeight);
        for (int i = 0; i < NowActor.Targets.Count; ++i)
            NowActor.Targets[i].SetAnimeSpeed(end, lerpWeight);
    }


    //## Field
    public static void Field_PlayerMoveTo(int input)
    {
        int mx = 0, mz = 0;

        if ((input & IDxINPUT.UP) != 0)
            mz += 1;
        if ((input & IDxINPUT.DOWN) != 0)
            mz -= 1;
        if ((input & IDxINPUT.RIGHT) != 0)
            mx += 1;
        if ((input & IDxINPUT.LEFT) != 0)
            mx -= 1;

        Vector3 move = MyPC.MoveTo(new Vector3(mx, 0, mz));
        CameraMgr.FollowPC(move);
    }
}

//Not Used
//public static void Battle_OrderByShellSort()
//{
//    int count = unitInBattle.Count;
//    Unit compareTo, swap;
//    for (int gap = (count >> 1); gap > 0; gap >>= 1)
//    {
//        for (int i = gap; i < count; ++i)
//        {
//            compareTo = unitInBattle[i];
//            int j;
//            for (j = i; j >= gap && unitInBattle[j - gap].Priority < compareTo.Priority; j -= gap)
//            {
//                swap = unitInBattle[j];
//                unitInBattle[j] = unitInBattle[j - gap];
//                unitInBattle[j - gap] = swap;
//            }
//            unitInBattle[j] = compareTo;
//        }
//    }
//}