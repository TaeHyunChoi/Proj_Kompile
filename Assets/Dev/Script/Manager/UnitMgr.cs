using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitMgr : MonoBehaviour
{
    public static UnitMgr Instance { get; private set; }

    public const byte GROUP_PLY  = 0;   //Player Group
    public const byte GROUP_ENM  = 1;   //Enemy Group
    public const byte GROUP_NPC  = 2;   //NPC Group

    public static Dictionary<int, Unit> UnitList { get => unitList; }
    private static Dictionary<int, Unit> unitList; //HashCode의 이점이 크게 없어진 기분쓰...

    private static Transform tfPlayer;
    private static Transform tfEnemy;

    public static Unit MyPC { get => myPC; }
    private static Unit myPC;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        SetUnitList();

        tfPlayer    = transform.GetChild(0);
        tfEnemy     = transform.GetChild(1);
        myPC        = tfPlayer.GetChild(0).GetComponent<Unit>();
    }
    public void SetUnitList()
    {
        unitList = new Dictionary<int, Unit>();

        //Group: Players
        Unit[] units = transform.GetChild(0).GetComponentsInChildren<Unit>();
        for (int i = 0; i < units.Length; ++i)
            unitList.Add(units[i].GetHashCode(), units[i]);


        //Group: Enemy
        units = transform.GetChild(1).GetComponentsInChildren<Unit>();
        for (int i = 0; i < units.Length; ++i)
            unitList.Add(units[i].GetHashCode(), units[i]);
    }


    public static void SetBattlePosition(byte type)
    {
        List<Unit> group = unitList.Values.Where(x => x.Data.Group == type).ToList();

        //맵 위치 띄우기
        float vx, vz, delta;
        if (type == 0)
        {
            tfPlayer.position += Vector3.up * 100;
            vx = -4f; ;     //x축 기준
            vz = -3.3f;     //z축 기준

            switch (group.Count)
            {
                case 1:
                    delta = -1.5f;
                    group[0].transform.localPosition = new Vector3(vx, 0, vz + delta);
                    break;
                case 2:
                    delta = -1f;
                    group[0].transform.localPosition = new Vector3(vx, 0, vz + delta);
                    group[1].transform.localPosition = new Vector3(vx, 0, vz + delta * 2);
                    break;
                case 3:
                    delta = -1.5f;
                    group[0].transform.localPosition = new Vector3(vx, 0, vz);
                    group[1].transform.localPosition = new Vector3(vx, 0, vz + delta);
                    group[2].transform.localPosition = new Vector3(vx, 0, vz + delta * 2);
                    break;
            }
        }
        if (type == 1)
        {
            tfEnemy.position += Vector3.up * 100;
            //x축은 직접 입력
            vz = -3f;   //z축 기준

            switch (group.Count)
            {
                case 1:
                    delta = -1.325f;
                    group[0].transform.localPosition = new Vector3(4.5f, 0, vz + delta);
                    break;
                case 2:
                    delta = -1.25f;
                    group[0].transform.localPosition = new Vector3(4.5f, 0, vz + delta);
                    group[1].transform.localPosition = new Vector3(4.5f, 0, vz + delta * 2);
                    break;
                case 3:
                    delta = -1.25f;
                    group[0].transform.localPosition = new Vector3(4f, 0, vz + delta);
                    group[1].transform.localPosition = new Vector3(4.5f, 0, vz + delta * 2);
                    group[2].transform.localPosition = new Vector3(4f, 0, vz + delta * 3);
                    break;
                case 4:
                    delta = -1.25f;
                    group[0].transform.localPosition = new Vector3(4f, 0, vz);
                    group[1].transform.localPosition = new Vector3(4.5f, 0, vz + delta);
                    group[2].transform.localPosition = new Vector3(4.5f, 0, vz + delta * 2);
                    group[3].transform.localPosition = new Vector3(3.5f, 0, vz + delta * 3);
                    break;
            }
        }
    }
    public static void PlayerMoveTo(InputKey input)
    {
        int mx = 0, mz = 0;

        if ((input & InputKey.Up) == InputKey.Up)
            mz += 1;
        if ((input & InputKey.Down) == InputKey.Down)
            mz -= 1;
        if ((input & InputKey.Right) == InputKey.Right)
            mx += 1;
        if ((input & InputKey.Left) == InputKey.Left)
            mx -= 1;

        MyPC.MoveTo(new Vector3(mx, 0, mz));
    }


    public static int GetGroupCount(byte type)
    {
        return unitList.Values.Where(x => x.Data.Group == type).ToList().Count;
    }


    public static List<SkillData> GetSkillTypeof(Unit unit, int type)
    {
        //해당 타입의 스킬 모두 추가
        List<SkillData> result = unit.Skill.FindAll(x => (x.SkillGroup == type));

        //특수기 > 공통스킬 추가
        if (type == 6) 
            result.AddRange(DataMgr.SkillTBL.FindAll(x => (x.ActorIndex == 0xF)));

        return result;
    }
    public static SkillData GetSkill(Unit unit, int type, int index)
    {
        return unit.Skill.FindAll(x => (x.SkillGroup == type + 1))[index];
    }
}
