using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitMgr
{
    public static Dictionary<int, Unit> AllUnits { get => allUnits; }
    private static Dictionary<int, Unit> allUnits = new Dictionary<int, Unit>();

    public static List<Unit> BattleUnits { get => battleUnits; }
    public static List<Unit> battleUnits = new List<Unit>();

    private static Transform tfActive;
    private static Transform tfInactive;

    public static Unit MyPC { get => myPC; }
    public static Unit myPC;

    public static void Init(Transform tf)
    {
        tfActive    = tf.GetChild(0);
        tfInactive  = tf.GetChild(1);
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

        allUnits.Add(newUnit.gameObject.GetHashCode(), newUnit);
        return newUnit;
    }
    public static void SetMyPC(int unitCode)
    {
        myPC = GetUnitByCode(unitCode);
    }


    public static void Battle_SetUnitData(MapData map)
    {
        List<Unit> units = GetUnitGroup(IDxUNIT.PLAYER);
        battleUnits.AddRange(units);

        int count = Random.Range(map.MinCount, map.MaxCount); //¸Ê Mob °³¼ö
        byte index;
        units.Clear();
        for (int i = 0; i < count; ++i)
        {
            index = map.Mob[Random.Range(0, map.Mob.Length)];
            units.Add(New(index, Vector3.zero));
        }
        battleUnits.AddRange(units);

        //Set Battle Stats
        for (int i = 0; i < battleUnits.Count; ++i)
            battleUnits[i].SetBattleStat();
    }
    public static void Battle_SetPosition()
    {
        float delta;
        Vector3 standard;
        List<Unit> group;

        group = battleUnits.FindAll(x=>x.Data.Group == IDxUNIT.PLAYER);
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

        group = battleUnits.FindAll(x => x.Data.Group == IDxUNIT.ENEMY);
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
    public static List<int> Battle_GetUnitHashCodes()
    {
        List<int> hash = new List<int>();
        for (int i = 0; i < battleUnits.Count; ++i)
            hash.Add(battleUnits[i].HashCode);

        return hash;
    }
    public static void Battle_UnitReorder(List<int> hash)
    {
        List<Unit> reorder = new List<Unit>();
        foreach (int code in hash)
        {
            foreach (Unit unit in battleUnits)
            {
                if (code == unit.HashCode)
                {
                    reorder.Add(unit);
                    break;
                }
            }
        }
        battleUnits.Clear();
        battleUnits = reorder;

        foreach (var unit in battleUnits)
            Debug.Log($"[{unit.Data.Name}] {unit.BattleSpeed:F2}");

    }



    public static void PlayerMoveTo(int input)
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
    public static int GetGroupCount(byte type)
    {
        return allUnits.Values.Where(x => x.Data.Group == type).ToList().Count;
    }
    public static List<Unit> GetUnitGroup(byte type)
    {
        return allUnits.Values.Where(unit => unit.Data.Group == type).ToList();
    }

    public static Unit GetUnitByCode(int unitCode)
    {
        int hash = allUnits.First(x => x.Value.Data.Code == unitCode).Key;
        return allUnits[hash];
    }
    public static List<SkillData> GetSkillTypeof(Unit unit, int type)
    {
        return unit.Skill.FindAll(x => (x.SkillGroup == type));
    }
    public static SkillData GetSkill(Unit unit, int type, int index)
    {
        return unit.Skill.FindAll(skill => skill.SkillGroup == type)[index];
    }

    public static void CallBattleAction(int hash)
    {
        //[Delegate] PLY => UI ¶ç¿ö¶ó, ENM => AI µ¹·Á¶ó
        allUnits[hash].Battle();
    }
}