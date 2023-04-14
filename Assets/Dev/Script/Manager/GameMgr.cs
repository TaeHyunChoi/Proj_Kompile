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
        UnitMgr.Battle_SetUnit(NowMap);

        //Start Battle Process
        UnitMgr.Battle_SelectAction(nowOrder = 0);
    }
    public static void Battle_NextTurn()
    {
        //End Battle

        //End All Units Turn
        
        //Next Trun
        UnitMgr.Battle_SelectAction(++nowOrder);
    }
    private static void Battle_End()
    { 
        
    }
}