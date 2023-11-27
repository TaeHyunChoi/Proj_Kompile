using UnityEngine;

public class Main : MonoBehaviour//갑자기 얘가 엄청 마음에 안드는데? 뭐라고 객체가 되어야 하나;
{
    private Main instance;
    public  int testMapCode;

    public enum TestMode : short
    { 
        Field   = IDxSTATE.FIELD,
        Battle  = IDxSTATE.BATTLE_PLY_MENU,
    }
    public TestMode inputMode;


    private void Awake()
    {
        //## Instancing
        if (instance != null)
            return;

        instance = this;

        //## Get RawData
        DataMgr.LoadCSVTable();
        ResourceMgr.LoadAssetFromRcs();

        //CameraMgr.Init(transform.Find("Camera"));
        //UIMgr.Init(transform.Find("UI"));
        //UnitMgr.Init(transform.Find("Unit"));

        //## Set GameData
        Player.Init();

        //## Test
        TestSetting();
    }

    private void Update()
    {
        //InputMgr.Update();
    }

    private void TestSetting()
    {
        Player.Test();
        GameMgr.MapData_Change(testMapCode);
        GameMgr.BattleProc_Enter();
    }
}