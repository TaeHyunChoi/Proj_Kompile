using UnityEngine;

public partial class Main : MonoBehaviour
{
    [SerializeField]
    private int frameRate = 60; //얘는 Main.cs가 아니라 Config.cs로 빠지는 게 맞는 듯?
    public static int FrameRate { get => instance.frameRate; }

    private static Main instance;
    private static UnitPlayer player;

    private InputMgr mgrInput;
    private UnitMgr     mgrUnit;
    private UIMgr       mgrUI;
    private SceneMgr    mgrScene;
    private CameraFollow cam;

    public static Main     Instance { get => instance; }
    public static UnitPlayer Player { get => player; }
    public static InputMgr InputMgr { get => instance.mgrInput; }
    public static SceneMgr SceneMgr { get => instance.mgrScene; }
    public static UnitMgr  UnitMgr  { get => instance.mgrUnit; }
    public static UIMgr    UIMgr    { get => instance.mgrUI; }
    public static CameraFollow Cam { get => instance.cam; }

    private ContentBase content;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        DataTable.LoadTable();
        //++Load Player Data
        mgrInput = transform.GetComponent<InputMgr>();
        mgrUI    = new UIMgr   (transform.Find("UI"));
        mgrUnit  = new UnitMgr (transform.Find("Unit"));
        mgrScene = new SceneMgr(transform.Find("Scene"));
        cam      = Camera.main.transform.GetComponent<CameraFollow>();
    }
    private void Start()
    {
        mgrScene.LoadSceneAsync(GameState.Opening);
        Application.targetFrameRate = frameRate;
    }

    //Main은 Mgr급만 상대한다! 느낌인디
    //inputGetter가 그정도는 아닌 듯.

    private void Update()
    {
        //if (true == InputMgr.TryGetInput(out int input)
        //    && null != inputGetter)
        //{
        //    inputGetter.Input(input);
        //}
    }
    private void FixedUpdate()
    {
        //if (true == InputMgr.TryGetInput(out int input)
        //    && null != fixedInputGetter)
        //{
        //    fixedInputGetter.FixedInput(input);
        //}
    }


    public void SetContent(ContentBase content)
    {
        this.content = content;
        InputMgr.SetInputGetter(content as IGetInput);
    }
    public T GetContent<T>() where T:ContentBase
    {
        return content as T;
    }
    public void Release()
    {
        if (null != content)
        {
            content.Dispose();
        }

        UIMgr.Release();
    }

    public void SetPlayer(UnitPlayer unit)
    {
        player = unit;
    }
}
