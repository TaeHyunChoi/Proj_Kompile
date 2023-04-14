using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMgr
{
    //## Map
    private static MapData NowMap;
    private static MapData LastMap;
    private static Vector3 lastFieldPos;

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
        //Set Map
        CameraMgr.OnBattleCam(true);
        ChangeMapData(mapCode: NowMap.BattleMapCode);

        //Set Unit Data
        UnitMgr.Battle_InitUnit(NowMap);
        UnitMgr.Battle_SetUnit(NowMap);    

        //Start Battle Process
        UnitMgr.Battle_SelectAction(nowOrder = 0);
        Debug.Log($"Order[{nowOrder}] {UnitMgr.Battle_GetUnit(nowOrder)}");
    }
    public static void Battle_NextTurn()
    {
        //End Battle
        if (UnitMgr.IsEndBattle())
        {
            Debug.Log("End Battle");
            return;
        }


        //End All Units Turn: 전투불능되면 그냥 Remove로 날려야겠다
        if (UnitMgr.IsEndCycle(nowOrder))
        {
            UnitMgr.Battle_SetUnit(NowMap);
            nowOrder = -1;
            Debug.Log($"New Cycle");
        }

        //Next Trun
        UnitMgr.Battle_SelectAction(++nowOrder);
        Debug.Log($"Order[{nowOrder}] {UnitMgr.Battle_GetUnit(nowOrder)}");
    }
    private static void Battle_End()
    { 
        
    }
}