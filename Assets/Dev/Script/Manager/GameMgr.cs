using System.Collections.Generic;
using UnityEngine;

public class GameMgr
{
    //## Map
    public static MapData NowMap;
    public static MapData LastMap;
    private static Vector3 lastFieldPos;

    //## Battle
    private static List<int> BattleOrder = new List<int>();

    //## 뭔가 지저분한데? 정리해보자.
    public static Unit BattleUnits { get => UnitMgr.Units[BattleOrder[nowOrder]]; }
    private static int nowOrder;
    public static Unit GetUnitByOrder(int index)
    {
        return UnitMgr.Units[BattleOrder[index]];
    }


    public static void EnterBattle()
    {
        lastFieldPos = UnitMgr.MyPC.Pos;

        CameraMgr.OnBattleCam(true);
        NowMap = ChangeMapData(mapCode: NowMap.BattleMapCode);
        //Set Map Rcs
        BattleOrder.Clear();

        SetBattleUnitData();
        OrderByShellSort(out BattleOrder);

        InputMgr.Set(InputMode.Battle_Menu);

        //Loop Battle Systems
    }
    private static MapData ChangeMapData(ushort mapCode)
    {
        LastMap = NowMap;
        return DataMgr.MapTBL.Find(map => map.Code == mapCode);
    }
    public static void SetBattleUnitData()
    {
        //##Players
        List<Unit> units = UnitMgr.GetUnitGroup(UnitMgr.GROUP_PLY);
        for (int i = 0; i < units.Count; ++i)
        {
            BattleOrder.Add(units[i].gameObject.GetHashCode());
            units[i].SetBattleStat();
        }

        
        UnitMgr.SetBattlePosition(UnitMgr.GROUP_PLY, units);
        //UnitMgr로 일괄 처리하는 방법도?

        //##Enemies
        int count = UnityEngine.Random.Range(NowMap.MinCount, NowMap.MaxCount);        //맵 Mob 개수
        int mobVariation = NowMap.Mob.Length;
        byte index;

        units.Clear();
        for (int i = 0; i < count; ++i)
        {
            index = NowMap.Mob[Random.Range(0, mobVariation)];
            units.Add(UnitMgr.New(index, Vector3.zero));
            units[i].SetBattleStat();
            BattleOrder.Add(units[i].gameObject.GetHashCode());
        }
        UnitMgr.SetBattlePosition(UnitMgr.GROUP_ENM, units);
    }
    private static void OrderByShellSort(out List<int> hash)
    {
        hash = new List<int>();
        hash.AddRange(UnitMgr.Units.Keys);

        //Set Battle Speed
        int count = hash.Count;
        //for (int i = 0; i < count; ++i)
        //    UnitMgr.Units[hash[i]].SetBattleSpeed();

        //Ordered by Shell Sort
        for (int gap = (count >> 1); gap > 0; gap >>= 1)
        {
            for (int i = gap; i < count; ++i)
            {
                Unit temp = UnitMgr.Units[hash[i]];

                int j;
                for (j = i; j >= gap && UnitMgr.Units[hash[j - gap]].BattleSpeed < temp.BattleSpeed; j -= gap)
                {
                    //Swap
                    hash[j] ^= hash[j - gap];
                    hash[j - gap] ^= hash[j];
                    hash[j] ^= hash[j - gap];
                }

                UnitMgr.Units[hash[j]] = temp;
            }
        }
    }
}
