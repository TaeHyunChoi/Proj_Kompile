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
    private static int[] remainUnits = new int[2];  //각 그룹별 남아있는 유닛 수

    //## 뭔가 지저분한데? 정리해보자.
    public static Unit BattleUnits { get => UnitMgr.Units[BattleOrder[nowOrder]]; }
    private static int nowOrder;
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
        Battle_OrderByShellSort(out BattleOrder);

        //Loop Battle Systems
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
                    hash[j] ^= hash[j - gap];
                    hash[j - gap] ^= hash[j];
                    hash[j] ^= hash[j - gap];
                }

                UnitMgr.Units[hash[j]] = temp;
            }
        }
    }
    private static void Battle_NextTurn()
    {
        //전투 종료 판단
        //Player or Enemy All Dead
        //그냥 변수로 취급하는게 좋으려나?

        //1회전이 끝났는지 판단해야 하고
        if (BattleOrder.Count <= 0)
        { 

        }

        //1회전 중이라면 다음턴을 넘거야 하고
        UnitMgr.CallBattleAction(BattleOrder[nowOrder]);
        ++nowOrder;
    }
}
