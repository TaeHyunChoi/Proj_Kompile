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
        return UnitMgr.AllUnits[BattleOrder[index]];
    }
    private static MapData ChangeMapData(ushort mapCode)
    {
        LastMap = NowMap;
        return DataMgr.MapTBL.Find(map => map.Code == mapCode);
    }


    public static void Battle_Enter()
    {
        //Set Map
        CameraMgr.OnBattleCam(true);
        lastFieldPos = UnitMgr.MyPC.Pos;
        NowMap = ChangeMapData(mapCode: NowMap.BattleMapCode);

        //Set Unit Data
        UnitMgr.Battle_SetUnitData(NowMap);
        UnitMgr.Battle_SetPosition();

        //Sort by Speed
        Battle_OrderByShellSort(out BattleOrder);

        //Next
        Battle_NextTurn();
    }
    private static void Battle_OrderByShellSort(out List<int> hash)
    {
        //Set Battle Speed
        hash = UnitMgr.Battle_GetUnitHashCodes();

        //Ordered by Shell Sort
        int count = hash.Count;
        for (int gap = (count >> 1); gap > 0; gap >>= 1)
        {
            for (int i = gap; i < count; ++i)
            {
                Unit temp = UnitMgr.AllUnits[hash[i]];

                int j;
                for (j = i; j >= gap && UnitMgr.AllUnits[hash[j - gap]].BattleSpeed < temp.BattleSpeed; j -= gap)
                {
                    //Swap
                    hash[j]       ^= hash[j - gap];
                    hash[j - gap] ^= hash[j];
                    hash[j]       ^= hash[j - gap];
                }

                UnitMgr.AllUnits[hash[j]] = temp;
            }
        }

        //Re-order BattleUnit
        UnitMgr.Battle_UnitReorder(hash);
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