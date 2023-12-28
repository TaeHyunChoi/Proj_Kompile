using UnityEngine;

public class Main : MonoBehaviour
{
    private static Main instance;

    private static UIManager    uiMgr;
    private static InputManager inputMgr;
    private static GameManager  gameMgr;

    //얘 너무 마음에 안 드는데 흠...
    public static UIManager    UIMgr    { get => uiMgr; }
    public static InputManager InputMgr { get => inputMgr; }
    public static GameManager  GameMgr  { get => gameMgr; }

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        DataTable.LoadTable();
        //++Load Player Data

        inputMgr = new InputManager();
        uiMgr    = new UIManager(transform.Find("UI"));
        gameMgr  = new GameManager(transform.Find("Ingame"));
    }
    private void Start()
    {
        gameMgr.InitContent(ContentType.Opening);
    }
    private void Update()
    {
        inputMgr.Update();
        gameMgr. Update();
        uiMgr.   Update();
    }
}