using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitMgr
{
    private static List<Unit> unitAll      = new List<Unit>();
    private static List<Unit> unitBattle   = new List<Unit>();

    private static Transform tfActive;
    private static Transform tfInactive;

    public static Unit MyPC { get => myPC; }
    public static Unit myPC;

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
    public static void Battle_SetUnit(MapData map)
    {
        Battle_SetUnitData(map);    //전투 참여하는 유닛 리스트 생성
        Battle_OrderByShellSort();  //전투 순서에 따라 정렬
        Battle_SetPosition();       //화면 상의 전투 위치 설정
    }
    public static void Battle_SetUnitData(MapData map)
    {
        List<Unit> units = unitAll.FindAll(unit => unit.Data.Group == IDxUNIT.PLAYER);
        unitBattle.AddRange(units);

        int count = Random.Range(map.MinCount, map.MaxCount); //맵 Mob 개수
        byte index;
        units.Clear();
        for (int i = 0; i < count; ++i)
        {
            index = map.Mob[Random.Range(0, map.Mob.Length)];
            units.Add(New(index, Vector3.zero));
        }
        unitBattle.AddRange(units);

        //Set Battle Stats
        for (int i = 0; i < unitBattle.Count; ++i)
            unitBattle[i].Battle_SetStat();
    }
    public static void Battle_OrderByShellSort()
    {
        int count = unitBattle.Count;
        Unit compareTo, swap;
        for (int gap = (count >> 1); gap > 0; gap >>= 1)
        {
            for (int i = gap; i < count; ++i)
            { 
                compareTo = unitBattle[i];
                int j;
                for (j = i; j >= gap && unitBattle[j - gap].Priority < compareTo.Priority; j -= gap)
                {
                    swap = unitBattle[j];
                    unitBattle[j] = unitBattle[j - gap];
                    unitBattle[j - gap] = swap;
                }
                unitBattle[j] = compareTo;
            }
        }
    }
    public static void Battle_SetPosition()
    {
        float delta;
        Vector3 standard;
        List<Unit> group;

        group = unitBattle.FindAll(x => x.Data.Group == IDxUNIT.PLAYER);
        standard = new Vector3(-4f, 100f, -3.3f);
        switch (group.Count)
        {
            case 1:
                delta = -1.5f;
                group[0].transform.localPosition = standard + new Vector3(0, 0, delta);
                break;
            case 2:
                delta = -1f;
                group[0].transform.localPosition = standard + new Vector3(0, 0, delta);
                group[1].transform.localPosition = standard + new Vector3(0, 0, delta * 2);
                break;
            case 3:
                delta = -1.5f;
                group[0].transform.localPosition = standard;
                group[1].transform.localPosition = standard + new Vector3(0, 0, delta);
                group[2].transform.localPosition = standard + new Vector3(0, 0, delta * 2);
                break;
        }

        group = unitBattle.FindAll(x => x.Data.Group == IDxUNIT.ENEMY);
        standard = new Vector3(4.5f, 100f, -3f);
        switch (group.Count)
        {
            case 1:
                delta = -1.325f;
                group[0].transform.localPosition = standard + new Vector3(0, 0, delta);
                break;
            case 2:
                delta = -1.25f;
                group[0].transform.localPosition = standard + new Vector3(0, 0, delta);
                group[1].transform.localPosition = standard + new Vector3(0, 0, delta * 2);
                break;
            case 3:
                delta = -1.25f;
                group[0].transform.localPosition = standard + new Vector3(-0.5f, 0, delta);
                group[1].transform.localPosition = standard + new Vector3(0, 0, delta * 2);
                group[2].transform.localPosition = standard + new Vector3(-0.5f, 0, delta * 3);
                break;
            case 4:
                delta = -1.25f;
                group[0].transform.localPosition = standard = new Vector3(-0.5f, 0, 0);
                group[1].transform.localPosition = standard + new Vector3(0, 0, delta);
                group[2].transform.localPosition = standard + new Vector3(0, 0, delta * 2);
                group[3].transform.localPosition = standard + new Vector3(-1f, 0, delta * 3);
                break;
        }
    }


    //## Battle > Unit Data (for Action)
    public static Unit Battle_GetUnit(int order)
    {
        return unitBattle[order];
    }
    public static List<SkillData> Battle_GetSkillTypeof(int order, int type)
    {
        return unitBattle[order].Skill[type];
    }
    public static SkillData Battle_GetSkill(int order, int type, int index)
    {
        return unitBattle[order].Skill[type][index];
    }
    public static void Battle_SaveUnitAction(int order, int act)
    {
        unitBattle[order].Battle_SaveLastAction(act);
    }


    //## Battle > UI
    public static int Battle_SetTarget(int group, int targetIndex)
    {
        //타겟팅 선택을 취소할 때에 모든 unitBattle.beTargeted(false)로 처리하면 되는데;
        //아직 기능이 없으니 일단 for()문으로 끈다...

        switch (group)
        {
            case IDxUNIT.TARGET_ENM_SOLO:
            case IDxUNIT.TARGET_PLY_SOLO:
                {
                    //TargetGroup index, Group index 서로 다름
                    group = (group == IDxUNIT.TARGET_ENM_SOLO) ? IDxUNIT.ENEMY : IDxUNIT.PLAYER;

                    for (int i = 0; i < unitBattle.Count; ++i)
                    {
                        if (unitBattle[i].Data.Group == group)
                        {
                            //(예외적으로) 최초 세팅의 경우?
                            if (targetIndex <= 0)
                                targetIndex = i;

                            unitBattle[i].BeTargeted(i == targetIndex);
                            break;
                        }
                    }

                    break;
                }
            case IDxUNIT.TARGET_ENM_ALL:
            case IDxUNIT.TARGET_PLY_ALL:
                {
                    group = (group == IDxUNIT.TARGET_ENM_ALL) ? IDxUNIT.ENEMY : IDxUNIT.PLAYER;

                    List<Unit> groupUnits = unitBattle.FindAll(unit => unit.Data.Group == group);
                    for (int i = 0; i < groupUnits.Count; ++i)
                        groupUnits[i].BeTargeted(true);
                    break;
                }
            case IDxUNIT.TARGET_SELF:
                {
                    unitBattle[GameMgr.NowOrder].BeTargeted(true);
                    break;
                }
            case IDxUNIT.TARGET_XOR_SELF:
                {
                    for (int i = 0; i < unitBattle.Count; ++i)
                        unitBattle[i].BeTargeted(i != GameMgr.NowOrder);
                    break;
                }
        }

        return targetIndex;
    }
    public static void Battle_ResetTarget(int group, int targetIndex)
    {
        switch (group)
        {
            case IDxUNIT.TARGET_ENM_SOLO:
            case IDxUNIT.TARGET_PLY_SOLO:
                {
                    unitBattle[targetIndex].BeTargeted(false);
                    break;
                }
            case IDxUNIT.TARGET_ENM_ALL:
            case IDxUNIT.TARGET_PLY_ALL:
                {
                    //TargetGroup index, Group index 서로 다름
                    group = (group == IDxUNIT.TARGET_ENM_ALL) ? IDxUNIT.ENEMY : IDxUNIT.PLAYER;

                    List<Unit> groupUnits = unitBattle.FindAll(unit => unit.Data.Group == group);
                    for (int i = 0; i < groupUnits.Count; ++i)
                        groupUnits[i].BeTargeted(false);
                    break;
                }
            case IDxUNIT.TARGET_SELF:
                {
                    unitBattle[GameMgr.NowOrder].BeTargeted(false);
                    break;
                }
            case IDxUNIT.TARGET_XOR_SELF:
                {
                    //하나 정도는 걍 중복 처리하자...
                    for (int i = 0; i < unitBattle.Count; ++i)
                        unitBattle[i].BeTargeted(false);
                    break;
                }
        }
    }

    //## Battle > Call Battle Action
    public static void Battle_CallAction(int order)
    {
        //[Delegate] PLY => UI 띄워라, ENM => AI 돌려라
        unitBattle[order].Battle();
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

        //if ((input & IDxINPUT.UP) == IDxINPUT.UP)
        //    mz += 1;
        //if ((input & IDxINPUT.DOWN) == IDxINPUT.DOWN)
        //    mz -= 1;
        //if ((input & IDxINPUT.RIGHT) == IDxINPUT.RIGHT)
        //    mx += 1;
        //if ((input & IDxINPUT.LEFT) == IDxINPUT.LEFT)
        //    mx -= 1;

        MyPC.MoveTo(mx, mz);
    }
}