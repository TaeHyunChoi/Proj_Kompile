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
        DataMgr.LoadAssetFromRcs();

        //For Test
        Player.TempItem();
        GameMgr.NowMap = DataMgr.MapTBL.Find(map => map.Code == 1000);

        InputMgr.Set(TestInputType);
    }

    private void Update()
    {
        InputMgr.Update();
    }
}