using UnityEngine;

public class x_Main : MonoBehaviour//갑자기 얘가 엄청 마음에 안드는데? 뭐라고 객체가 되어야 하나;
{
    private x_Main instance;
    public  int testMapCode;

    public enum TestMode : short
    { 
        Field   = x_IDxSTATE.FIELD,
        Battle  = x_IDxSTATE.BATTLE_PLY_MENU,
    }
    public TestMode inputMode;


    private void Awake()
    {
        //## Instancing
        if (instance != null)
            return;

        instance = this;

        //## Get RawData
        //DataMgr.LoadCSVTable();
        x_DataMgr.LoadTable();
        x_ResourceMgr.LoadAssetFromRcs();

        //CameraMgr.Init(transform.Find("Camera"));
        //UIMgr.Init(transform.Find("UI"));
        //UnitMgr.Init(transform.Find("Unit"));

        //## Set GameData
        x_Player.Init();

        //## Test
        TestSetting();
    }

    private void Update()
    {
        //InputMgr.Update();
    }

    private void TestSetting()
    {
        x_Player.Test();
        x_GameMgr.MapData_Change(testMapCode);
        x_GameMgr.BattleProc_Enter();
    }
}