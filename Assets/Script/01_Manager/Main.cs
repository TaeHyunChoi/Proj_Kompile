using UnityEngine;

public partial class Main : MonoBehaviour
{
    [SerializeField]
    private int frameRate = 60;

    private static Main instance;
    private static UnitPlayer player;

    private UnitMgr     unitMgr;
    private UIMgr       mgrUI;
    private SceneMgr    mgrScene;
    private CameraFollow cam;

    public static Main     Instance { get => instance; }
    public static UnitPlayer Player { get => player; }
    public static SceneMgr SceneMgr { get => instance.mgrScene; }
    public static UnitMgr  UnitMgr  { get => instance.unitMgr; }
    public static UIMgr    UIMgr    { get => instance.mgrUI; }
    public static CameraFollow Cam { get => instance.cam; }

    //current;
    private ContentBase content;
    private IGetInput inputGetter;
    private IGetFixedInput fixedInputGetter;

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

        mgrUI    = new UIMgr   (transform.Find("UI"));
        unitMgr  = new UnitMgr (transform.Find("Unit"));
        mgrScene = new SceneMgr(transform.Find("Scene"));
        cam   = UnityEngine.Camera.main.transform.GetComponent<CameraFollow>();
    }
    private void Start()
    {
        mgrScene.LoadSceneAsync(GameState.Opening);
        Application.targetFrameRate = frameRate;
    }

    //입력 제어는 InputMgr에서 처리하는게 차라리 나을 듯?
    private void Update()
    {
        //입력 제어 : 버그 수정

        //TODO: not Update(), but event?
        if (true == InputMgr.TryGetInput(out int input)
            && null != inputGetter)
        {
            inputGetter.Input(input);
        }
    }
    private void FixedUpdate()
    {
        if (true == InputMgr.TryGetInput(out int input)
            && null != fixedInputGetter)
        {
            fixedInputGetter.FixedInput(input);
        }
    }


    public void SetContent(ContentBase content)
    {
        this.content = content;
    }
    public T GetContent<T>() where T:ContentBase
    {
        return (T)content;
    }
    public void Release()
    {
        if (null != content)
        {
            content.Dispose();
        }

        UIMgr.Release();
    }

    public void SetInputGetter(IGetInput getter)
    {
        inputGetter = getter;
    }
    public void SetFixedInputGetter(IGetFixedInput fixedGetter)
    {
        fixedInputGetter = fixedGetter;
    }
    public void ReleaseInputGetter()
    {
        inputGetter = null;
    }

    public void SetPlayer(UnitPlayer unit)
    {
        player = unit;
    }
}
