using UnityEngine;

public partial class Main : MonoBehaviour
{
    private static Main instance;
    private UIMgr       mgrUI;
    private SceneMgr    mgrScene;

    public static Main     Instance { get => instance; }
    public static UIMgr    UIMgr    { get => instance.mgrUI; }
    public static SceneMgr SceneMgr { get => instance.mgrScene; }

    //current;
    private ContentBase content;
    private UIBase      ui;
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

        mgrUI = new UIMgr(transform.Find("UI"));
        mgrScene = new SceneMgr(transform.Find("Scene"));
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
    public void SetInputGetter(IGetInput getter)
    {
        inputGetter = getter;
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
