using UnityEngine;

public partial class Main : MonoBehaviour
{
    private static Main instance;
    private UnitMgr     unitMgr;
    private UIMgr       mgrUI;
    private SceneMgr    mgrScene;
    private CameraFollow camera;

    public static Main     Instance { get => instance; }
    public static SceneMgr SceneMgr { get => instance.mgrScene; }
    public static UnitMgr  UnitMgr  { get => instance.unitMgr; }
    public static UIMgr    UIMgr    { get => instance.mgrUI; }
    public static CameraFollow Camera { get => instance.camera; }

    //current;
    private ContentBase content;
    private IGetInput inputGetter;

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
        camera   = UnityEngine.Camera.main.transform.GetComponent<CameraFollow>();
    }
    private void Start()
    {
        mgrScene.LoadSceneAsync(GameState.Opening);
    }
    private void Update()
    {
        //TODO: not Update(), but event?
        if (true == InputMgr.TryGetInput(out int input)
            && null != inputGetter)
        {
            inputGetter.Input(input);
        }
    }


    public void SetContent(ContentBase content)
    {
        this.content = content;
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
    public void ReleaseInputGetter()
    {
        inputGetter = null;
    }

    public void Dispose()
    {
        //TODO: Dispose() timing?
        if (content != null)
        {
            content.Dispose();
        }
    }
}
