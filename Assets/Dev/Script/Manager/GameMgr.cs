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

    public static int State { get => state; }
    private static int state;

    public static void ChangeMapData(ushort mapCode)
    {
        LastMap = NowMap;
        lastFieldPos = UnitMgr.MyPC.Pos;
        NowMap = DataMgr.MapTBL.Find(map => map.Code == mapCode);
    }

    public static void UpdateOrder(int order)
    {
        nowOrder = order;
    }

    public static void State_Set(int idxState)
    {
        state = idxState;
    }

    public static void Battle_Enter()
    {
        //Phase 1
        state = IDxSTATE.NONE;
        ChangeMapData(mapCode: NowMap.BattleMapCode);   //[입력] 전투 맵    
        UnitMgr.Proc_EnterBattle(NowMap);               //[처리] 전투 유닛
        Battle_NextTurn();                              //[처리] 전투 진행
        CameraMgr.OnBattleCam(true);                    //[출력] 전투 카메라
    }
    public static void Battle_NextTurn()
    {
        UnitMgr.Select_SetNextUnit();
    }
}