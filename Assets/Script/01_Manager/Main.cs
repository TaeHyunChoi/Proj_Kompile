using UnityEngine;

public class Main : MonoBehaviour
{
    //TODO: FrameRate는 추후에 Config.cs 등에게 넘기기
    [SerializeField]
    private int frameRate = 60; 

    //singleton
    private static Main instance;

    //manager
    private InputMgr     mgrInput;
    private UnitMgr      mgrUnit;
    private UIMgr        mgrUI;
    private SceneMgr     mgrScene;
    private CameraFollow camFollower;

    //content
    private UnitPlayer player;
    private ContentBase content;

    //getter
    public static Main          Instance { get => instance; }
    public static UnitPlayer    Player   { get => instance.player; }
    public static InputMgr      InputMgr { get => instance.mgrInput; }
    public static SceneMgr      SceneMgr { get => instance.mgrScene; }
    public static UnitMgr       UnitMgr  { get => instance.mgrUnit; }
    public static UIMgr         UIMgr    { get => instance.mgrUI; }
    public static CameraFollow  Cam      { get => instance.camFollower; }

    private void Awake()
    {
        //Singleton
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        //Init DataTable
        DataTable.LoadTable();
        //TODO: Load Player Saved Data

        //Init Manager
        mgrInput = transform.GetComponent<InputMgr>();
        mgrUI    = new UIMgr   (transform.Find("UI"));
        mgrUnit  = new UnitMgr (transform.Find("Unit"));
        mgrScene = new SceneMgr(transform.Find("Scene"));
        camFollower      = Camera.main.transform.GetComponent<CameraFollow>();
    }

    private void Start()
    {
        mgrScene.LoadSceneAsync(GameState.Opening);
        Application.targetFrameRate = frameRate;
    }

    // set func()
    public void SetContent(ContentBase content) //여기서 type을 받는게 깔끔하려나.. 아니면 하지 않거나?
    {
        this.content = content;

        //IInputHandler가 없으면 null을 반환 => 그대로 Release까지 가능
        mgrInput.SetUpdater(content as IInputHandler);
        mgrInput.SetFixedUpdater(content as IFixedInputHandler);
    }
    public void SetFieldLayer(int layer)
    {
        OnField field = content as OnField;
        UnityEngine.Assertions.Assert.IsNotNull(field, "field is null;");
        field.TransLayer(layer);
    }
    public void SetPlayer(UnitPlayer unit)
    {
        player = unit;
        camFollower.SetFollow(player.transform);
    }

    public void Release()
    {
        if (null != content)
        {
            content.Dispose();
        }

        UIMgr.Release();
    }
}
