using UnityEngine;

public class Main : MonoBehaviour
{
    public Main instance { private get; set; }
    public InputMode TestInputType;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;


        DataMgr.LoadCSVTable();
        ResourceMgr.LoadAssetFromRcs();


        UnitMgr.Init(transform.Find("Unit"));


        GameMgr.NowMap = DataMgr.MapTBL.Find(map => map.Code == 1000);
        InputMgr.Set(TestInputType);

        TestSetting();
    }

    //private void Update()
    //{
    //    InputMgr.Update();
    //}
    private void TestSetting()
    {
        UnitMgr.New(0, Vector3.zero);
        UnitMgr.New(1, Vector3.back);
        UnitMgr.New(2, Vector3.back * 2);


        Player.TempItem();
    }
}