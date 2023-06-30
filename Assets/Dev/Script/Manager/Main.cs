using UnityEngine;

public class Main : MonoBehaviour
{
    public Main Instance { private get; set; }
    public ushort testMapCode;

    public enum TestMode : short
    { 
        Field   = IDxSTATE.FIELD,
        Battle  = IDxSTATE.BATTLE_PLY_MENU,
    }
    public TestMode inputMode;


    private void Awake()
    {
        //## Instancing
        if (Instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        //## Get RawData
        DataMgr.LoadCSVTable();
        ResourceMgr.LoadAssetFromRcs();

        //## Init Managers : Find()·Î ÅüÃÆÀ½;
        CameraMgr.Init(transform.Find("Camera"));
        UIMgr.Init(transform.Find("UI"));
        UnitMgr.Init(transform.Find("Unit"));


        //## Set GameData
        Player.Init();

        //## Test
        TestSetting();
    }

    private void Update()
    {
        InputMgr.Update();
    }

    private void TestSetting()
    {
        Player.Test();
        GameMgr.MapData_Change(testMapCode);
        GameMgr.BattleProc_Enter();
    }
}