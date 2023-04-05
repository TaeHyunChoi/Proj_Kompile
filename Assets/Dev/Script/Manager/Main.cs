using UnityEngine;

public class Main : MonoBehaviour
{
    public Main instance { private get; set; }
    private InputMode NowInputMode;

    private void Awake()
    {
        //## Instancing
        if (instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;

        //## Get RawData
        DataMgr.LoadCSVTable();
        ResourceMgr.LoadAssetFromRcs();

        //## Init Managers
        CameraMgr.Init(transform.Find("Camera"));
        UnitMgr.Init(transform.Find("Unit"));

        //## Set GameData
        Player.Init();
        GameMgr.NowMap = DataMgr.MapTBL.Find(map => map.Code == 1000);
        InputMgr.Set(NowInputMode = InputMode.Base);
    }

    private void Update()
    {
        InputMgr.Update();
    }
}