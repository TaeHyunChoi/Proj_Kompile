using UnityEngine;

public class x_GameMgr
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

    public static void MapData_Change(int mapCode)
    {
        LastMap = NowMap;
        lastFieldPos = x_UnitMgr.MyPC.Pos;
        NowMap = x_DataMgr.MapTBL.Find(map => map.Code == mapCode);
    }

    public static void Order_Update(int order)
    {
        nowOrder = order;
    }

    public static void State_Set(int idxState)
    {
        state = idxState;
    }

    public static void BattleProc_Enter()
    {
        //Phase 1
        state = x_IDxSTATE.NONE;
        MapData_Change(mapCode: NowMap.BattleMapCode);   //[입력] 전투 맵    
        x_UnitMgr.BattleProc_Enter(NowMap);               //[처리] 전투 유닛
        x_UnitMgr.Select_SetNextUnit();                   //[처리] 전투 진행
        x_CameraMgr.OnBattleCam(true);                    //[출력] 전투 카메라
    }
    public static void Battle()
    {
        //[처리] 전투 종료 판단
        int defeatedGroup = x_UnitMgr.Battle_WhichGroupDefeat();
        if (defeatedGroup != -1)
        {
            Debug.Log($"End Battle(Lose Group : {defeatedGroup})");
            return;
        }

        //[처리] 다음 턴 진행
        x_UnitMgr.Select_SetNextUnit();
    }
}