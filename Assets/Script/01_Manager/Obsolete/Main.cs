using UnityEngine;

[System.Obsolete]
public class Main : MonoBehaviour
{
    public static Main          Instance { get; private set; }
    public static UnitPlayer    Player   { get; set; }
    public static InputMgr      InputMgr { get; private set; }
    public static UnitMgr       UnitMgr  { get; private set; }
    public static CameraFollow  Cam      { get; private set; }

    private ContentBase mContent;
    private void Awake()
    {
        //like Singleton
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        x_DataTable.LoadTable();
        //TODO: Load Player Saved Data

        InputMgr = transform.GetComponent<InputMgr>();
        Cam      = Camera.main.transform.GetComponent<CameraFollow>();
        UnitMgr  = new UnitMgr (transform.Find("Unit"));
    }
    private void Update()
    {
        UnitMgr.Update();
    }

    // set func()
    public void SetContent(ContentBase content)
    {
        mContent = content;
        InputMgr.Updater      = content as IInputHandler;
        InputMgr.FixedUpdater = content as IFixedInputHandler;
    }
    public void SetFieldLayer(int layer)
    {
        OnField field = mContent as OnField;
        UnityEngine.Assertions.Assert.IsNotNull(field, "field is null;");
        field.TransLayer(layer);
    }
    public void SetPlayer(UnitPlayer player)
    {
        UnityEngine.Assertions.Assert.IsNotNull(player, "null player;");
        Player = player;
        Cam.SetFollow(player.Transform);
    }
}
