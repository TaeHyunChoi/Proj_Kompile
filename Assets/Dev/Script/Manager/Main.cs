using UnityEngine;

public class Main : MonoBehaviour
{
    public Main Instance { private get; set; }
    public ushort testMapCode;

    public enum TestMode : byte
    { 
        Field   = IDxINPUT.MODE_FIELD,
        Battle  = IDxINPUT.MODE_BATTLE_MENU,
        Cheat   = IDxINPUT.MODE_CHEAT
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

        //## Set Input Mode
        InputMgr.Set(IDxINPUT.MODE_FIELD);

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
        GameMgr.ChangeMapData(testMapCode);
        InputMgr.Set(/*IDxINPUT.MODE_CHEAT*/ (byte)inputMode);
    }
}