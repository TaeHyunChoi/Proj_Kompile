using System.Collections.Generic;
using UnityEngine;

public class GameMgr
{
    //## Map
    public static MapData NowMap;
    public static MapData LastMap;
    private static Vector3 lastFieldPos;


    private static List<int> BattleOrder = new List<int>();
    public static int NowUnitHash { get => BattleOrder[nowOrder]; }
    private static int nowOrder;


    //각 그룹별 남아있는 유닛 수
    public static int[] Remains { get => remainUnits; } 
    private static int[] remainUnits = new int[2];  


    public static Unit GetUnitByOrder(int index)
    {
        return UnitMgr.Units[BattleOrder[index]];
    }
    private static MapData ChangeMapData(ushort mapCode)
    {
        LastMap = NowMap;
        return DataMgr.MapTBL.Find(map => map.Code == mapCode);
    }


    public  static void Battle_Enter()
    {
        lastFieldPos = UnitMgr.MyPC.Pos;

        CameraMgr.OnBattleCam(true);
        NowMap = ChangeMapData(mapCode: NowMap.BattleMapCode);
        //Set Map Rcs
        Battle_SetUnitData();

        //Init Set
        nowOrder = 0;
        Battle_OrderByShellSort(out BattleOrder);

        Battle_NextTurn();
    }
    public  static void Battle_SetUnitData()
    {
        //## Players
        List<Unit> units = UnitMgr.GetUnitGroup(IDxUNIT.PLAYER);
        remainUnits[IDxUNIT.PLAYER] = units.Count;
        for (int i = 0; i < units.Count; ++i)
        {
            BattleOrder.Add(units[i].gameObject.GetHashCode());
            units[i].SetBattleStat();
        }
        UnitMgr.SetBattlePosition(IDxUNIT.PLAYER, units);

        //##Enemies
        int count = UnityEngine.Random.Range(NowMap.MinCount, NowMap.MaxCount); //맵 Mob 개수
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
        remainUnits[IDxUNIT.ENEMY] = units.Count;
        UnitMgr.SetBattlePosition(IDxUNIT.ENEMY, units);
    }
    private static void Battle_OrderByShellSort(out List<int> hash)
    {
        hash = new List<int>();
        hash.AddRange(UnitMgr.Units.Keys);

        //Set Battle Speed
        int count = hash.Count;

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
                    hash[j]       ^= hash[j - gap];
                    hash[j - gap] ^= hash[j];
                    hash[j]       ^= hash[j - gap];
                }

                UnitMgr.Units[hash[j]] = temp;
            }
        }
    }
    public static void Battle_NextTurn()
    {
        //End Battle
        if (remainUnits[0] == 0 || remainUnits[1] == 0)
        {
            Debug.Log("End Battle >> Give out Reward");
            return;
        }

        //End All Units Turn
        if (BattleOrder.Count <= 0)
        {
            Battle_OrderByShellSort(out BattleOrder);
            nowOrder = 0;
        }

        //Next Trun
        UnitMgr.CallBattleAction(BattleOrder[nowOrder]);
    }
}