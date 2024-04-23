using UnityEngine;

public class Main : MonoBehaviour
{
    //TODO: FrameRate는 추후에 Config.cs 등에게 넘기기
    [SerializeField]
    private int mFrameRate = 144; 

    public static Main          Instance { get; private set; }
    public static UnitPlayer    Player   { get; set; }
    public static InputMgr      InputMgr { get; private set; }
    public static SceneMgr      SceneMgr { get; private set; }
    public static UnitMgr       UnitMgr  { get; private set; }
    public static UIMgr         UIMgr    { get; private set; }
    public static CameraFollow  Cam      { get; private set; }

    private ContentBase mContent;

    // Main.cs
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

        DataTable.LoadTable();
        //TODO: Load Player Saved Data

        InputMgr = transform.GetComponent<InputMgr>();
        Cam      = Camera.main.transform.GetComponent<CameraFollow>();
        UIMgr    = new UIMgr   (transform.Find("UI"));
        UnitMgr  = new UnitMgr (transform.Find("Unit"));
        SceneMgr = new SceneMgr(transform.Find("Scene"));
    }
    private void Start()
    {
        SceneMgr.LoadSceneAsync(EGameStateFlag.Opening);
        Application.targetFrameRate = mFrameRate;
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

    public void Release()
    {
        if (null != mContent)
        {
            mContent.Dispose();
        }

        UIMgr.Release();
    }
}
