using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public enum GameState
{ 
    Field,
    Battle,
    Event
}
public class GameMgr
{
    public static GameState State { get => state; }
    private static GameState state;

    public static MapData NowMap;

    private static List<int> BattleOrder; //HashCode 저장?
    public static Unit NowUnit { get => UnitMgr.UnitList[BattleOrder[nowOrder]]; }
    private static int nowOrder;
    private static Vector3 lastFieldPos;



    public static void EnterBattle()
    {
        //Save Field Position For Back To Field
        lastFieldPos = UnitMgr.MyPC.transform.localPosition;

        //Set Battle Field
        //Set Map Rcs
        CameraMgr.MainCam.transform.position = new Vector3(0f, 105f, -10f);

        //## Set Players
        //Get Player Unit Data
        UnitMgr.SetBattlePosition(UnitMgr.GROUP_PLY);

        //## Set Enemies
        //Select Enemy units From MapData
        //Set Enemy Unit Data
        SetEnemies(out List<Unit> enemies);

        //Set Battle Order
        OrderByShellSort(out BattleOrder);

        //Set Position : 공격 순서에 따라 위치를 조금씩 바꾸는 위트도?
        UnitMgr.SetBattlePosition(UnitMgr.GROUP_ENM);

        //Set Game State
        state = GameState.Battle;
        InputMgr.Set(InputMode.Battle_Menu);

        //Loop Battle Systems
    }
    private static void OrderByShellSort(out List<int> hash)
    {
        hash = new List<int>();
        hash.AddRange(UnitMgr.UnitList.Keys);

        //Set Battle Speed
        int count = hash.Count;
        for (int i = 0; i < count; ++i)
            UnitMgr.UnitList[hash[i]].SetBattleSpeed();

        //Ordered by Shell Sort
        for (int gap = (count >> 1); gap > 0; gap >>= 1)
        {
            for (int i = gap; i < count; ++i)
            {
                Unit temp = UnitMgr.UnitList[hash[i]];

                int j;
                for (j = i; j >= gap && UnitMgr.UnitList[hash[j - gap]].BattleSpeed < temp.BattleSpeed; j -= gap)
                {
                    //Swap
                    hash[j] ^= hash[j - gap];
                    hash[j - gap] ^= hash[j];
                    hash[j] ^= hash[j - gap];
                }

                UnitMgr.UnitList[hash[j]] = temp;
            }
        }
    }
    public static Unit GetUnitByOrder(int index)
    {
        return UnitMgr.UnitList[BattleOrder[index]];
    }

    public static void SetEnemies(out List<Unit> enemies)
    {
        enemies = new List<Unit>();

        MapData battleMap = DataMgr.MapTBL.Find(map => map.Code == NowMap.BattleMapCode);

        //맵 리소스 설정하고... (아마도 프리팹이지 않을까)

        int mobIndex = UnityEngine.Random.Range(0, battleMap.MobVariety);
        int totalLv = 0;
        int mobLv = 0;

        for (int i = 0; i < battleMap.MobVariety; ++i)
        {
            UnitData data = DataMgr.UnitTBL.Find(x => x.Code == mobIndex);
            mobLv = UnityEngine.Random.Range(battleMap.MinLv, battleMap.MaxLv + 1);

            if (totalLv + mobLv > battleMap.TotalLv)
                break;

            totalLv += mobLv;

            //설계 고려. 재활용할 예정이니까...
        }


        //BattleOrder에도 추가해야 하네?   
    }
}
