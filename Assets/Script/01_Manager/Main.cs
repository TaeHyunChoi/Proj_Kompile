using UnityEngine;

public partial class Main : MonoBehaviour
{
    private static Main     instance;
    private UIMgr         mgrUI;
    private SceneMgr        mgrScene;

    //하 이거 뭔가 마음에 안 드네 진짜...
    public static Main          Instance         { get => instance; }
    public static UIMgr       UIMgr       { get => instance.mgrUI; }
    public static SceneMgr      SceneMgr    { get => instance.mgrScene; }

    private GameState state;
    public GameState State { get => state; }

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

        state = GameState.None;

        mgrUI = new UIMgr(transform.Find("UI"));
        mgrScene = new SceneMgr(transform.Find("Scene"));

        //mgrInput = new x_InputMgr();
        //mgrGame = new x_GameManager(transform.Find("Ingame"));
    }
    private void Start()
    {
        mgrScene.LoadSceneAsync(state, GameState.Opening);
    }
    private void Update()
    {
        if (true == InputMgr.TryGetInput(out int input)
            && null != inputGetter)
        {
            inputGetter.Input(input);
        }
    }
    public void Dispose(GameState state)
    {
        content.Dispose();
        //mgrUI.Dispose(state);
    }

    public void SetContent(ContentBase content)
    {
        this.content = content;
    }
    public void SetInputGetter(IGetInput getter)
    {
        inputGetter = getter;
    }
}
