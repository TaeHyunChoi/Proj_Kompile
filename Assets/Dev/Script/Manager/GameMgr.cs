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
        nowOrder = 0;
        Battle_NextTurn();
    }
    public static void Battle_ProcAction(int order, SkillData skill, int targetSolo = 0)
    {
        List<int> targetIndexes = new List<int>();

        //모든 대상을 다시 가져와야 하는거구나?
        //UnitMgr.Battle_SetTarget() 방식으로?
        //다른 방법이 있는지 고민 필요

        //Debug.Log($"[{skill.Name}] {UnitMgr.Battle_GetUnit(order).Data.Name} => {UnitMgr.Battle_GetUnit(skill.TargetGroup, targetSolo).Data.Name}");

        //unitBattle[order]에게
        //skill 애니메이션 실행
        //전투를 어찌 구현하는게 좋을까?
        //끝나면 그 다음에 NextTurn인데...
    }
    public static void Battle_NextTurn()
    {
        //End Battle

        //End All Units Turn
        
        //Next Trun
        UnitMgr.Battle_CallAction(nowOrder);
    }
    private static void Battle_End()
    { 
        
    }
}