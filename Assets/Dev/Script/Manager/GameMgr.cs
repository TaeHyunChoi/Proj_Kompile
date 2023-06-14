using System.Collections.Generic;
using UnityEngine;

public class GameMgr
{
    //## Map
    private static MapData NowMap;
    private static MapData LastMap;
    private static Vector3 lastFieldPos;

    //## Battle
    public static int NowOrder { get => nowOrder; }
    private static int nowOrder;

    public static void ChangeMapData(ushort mapCode)
    {
        LastMap = NowMap;
        lastFieldPos = UnitMgr.MyPC.Pos;
        NowMap = DataMgr.MapTBL.Find(map => map.Code == mapCode);
    }

    public static void Battle_Enter()
    {
        ChangeMapData(mapCode: NowMap.BattleMapCode);               //전투 맵    
        UnitMgr.ProcUnit_EnterBattle(NowMap, nowOrder = 0);         //전투 유닛  
        CameraMgr.OnBattleCam(true);                                //전투 카메라
    }
    public static void Battle_NextTurn()
    {
        //End Battle
        if (UnitMgr.IsEndBattle())
        {
            Debug.Log("End Battle");
            return;
        }


        //End All Units Turn
        if (UnitMgr.IsEndCycle(nowOrder))
        {
            UnitMgr.Battle_SetOrder();
            nowOrder = -1;
            Debug.Log($"New Cycle");
        }

        //Next Trun
        UnitMgr.Battle_SelectAction(++nowOrder);
    }
    private static void Battle_End()
    { 
        
    }
}