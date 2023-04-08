using UnityEngine;

public class Main : MonoBehaviour
{
    public Main Instance { private get; set; }
    public ushort testMapCode;

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

        //## Init Managers
        CameraMgr.Init(transform.Find("Camera"));
        UnitMgr.Init(transform.Find("Unit"));

        //## Set GameData
        Player.Init();

        //## Set Input Mode
        InputMgr.Set(IDxINPUT.FIELD);

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
        InputMgr.Set(IDxINPUT.CHEAT);
    }
}