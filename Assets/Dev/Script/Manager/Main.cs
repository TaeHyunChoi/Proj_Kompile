using UnityEngine;

public class Main : MonoBehaviour
{
    public Main Instance { private get; set; }
    public int testMapCode;

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
        GameMgr.NowMap = DataMgr.MapTBL.Find(map => map.Code == testMapCode);
        InputMgr.Set(InputMode.Base);
    }

    private void Update()
    {
        InputMgr.Update();
    }
}