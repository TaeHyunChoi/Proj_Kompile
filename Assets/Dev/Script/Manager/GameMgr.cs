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
        ChangeMapData(mapCode: NowMap.BattleMapCode);   //전투맵 정보로 변경
        UnitMgr.Battle_InitUnitList(NowMap);            //전투에 참여하는 유닛 정보 생성, 추가
        UnitMgr.Battle_SetPosition();                   //화면 상의 전투 위치 설정
        CameraMgr.OnBattleCam(true);                    //전투 카메라 설정

        UnitMgr.Battle_SelectAction(nowOrder = 0);  //[0]번째 유닛 액션 선택
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
            UnitMgr.Battle_OrderByShellSort();
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