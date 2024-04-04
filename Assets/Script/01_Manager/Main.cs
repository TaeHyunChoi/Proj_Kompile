using UnityEngine;

public partial class Main : MonoBehaviour
{
    private static Main  instance;
    private UIManager    mgrUI;
    private InputManager mgrInput;
    private GameManager  mgrGame;
    private SceneManager mgrScene;

    public static Main          Get      { get => instance; }
    public static UIManager     UIMgr    { get => instance.mgrUI; }
    public static InputManager  InputMgr { get => instance.mgrInput; }
    public static GameManager   GameMgr  { get => instance.mgrGame; }
    public static SceneManager  SceneMgr { get => instance.mgrScene; }
    
    private GameState state;

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

        mgrInput = new InputManager();
        mgrGame  = new GameManager(transform.Find("Ingame"));
        mgrUI    = new UIManager(transform.Find("UI"));
        mgrScene = new SceneManager();
    }
    private void Start()
    {
        state = GameState.Opening;
        mgrScene.LoadSceneAsync(0);
    }
    private void Update()
    {
        int input = mgrInput.Update();
        mgrUI.Update();
        mgrGame .Update();
    }
    public void StartContent()
    {
        mgrGame.Start();
    }
    public void Dispose()
    {
        mgrGame.Dispose();
        mgrUI.Dispose(state);
        System.GC.Collect();
    }
}
