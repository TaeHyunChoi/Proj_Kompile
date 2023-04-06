using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitMgr
{
    public static Dictionary<int, Unit> Units { get => units; }
    private static Dictionary<int, Unit> units = new Dictionary<int, Unit>();

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

        units.Add(newUnit.gameObject.GetHashCode(), newUnit);

        return newUnit;
    }
    public static void SetMyPC(int unitCode)
    {
        myPC = GetUnitByCode(unitCode);
    }

    public static void SetBattlePosition(byte type, List<Unit> group = null)
    {
        if (group == null)
            group = units.Values.Where(x => x.Data.Group == type).ToList();

        float delta;
        Vector3 standard;
        if (type == 0)
        {
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
        }
        if (type == 1)
        {
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
                    group[2].transform.localPosition = standard + new Vector3(0,0, delta * 2);
                    group[3].transform.localPosition = standard + new Vector3(-1f, 0, delta * 3);
                    break;
            }
        }
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
        return units.Values.Where(x => x.Data.Group == type).ToList().Count;
    }
    public static List<Unit> GetUnitGroup(byte type)
    {
        return units.Values.Where(unit => unit.Data.Group == type).ToList();
    }

    public static Unit GetUnitByCode(int unitCode)
    {
        int hash = units.First(x => x.Value.Data.Code == unitCode).Key;
        return units[hash];
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
        //[Delegate] PLY => UI 띄워라, ENM => AI 돌려라
        units[hash].Battle();
    }
}