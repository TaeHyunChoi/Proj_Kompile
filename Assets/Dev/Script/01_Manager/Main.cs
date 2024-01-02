using Unity.VisualScripting;
using UnityEngine;

public class Main : MonoBehaviour
{
    private static Main         instance;
    private static UIManager    uiMgr;
    private static InputManager inputMgr;
    private static GameManager  gameMgr;
    private static LevelManager sceneMgr;

    public static Main         Instance { get => instance; }
    public static UIManager    UIMgr    { get => uiMgr; }
    public static InputManager InputMgr { get => inputMgr; }
    public static GameManager  GameMgr  { get => gameMgr; }
    public static LevelManager SceneMgr { get => sceneMgr; }

    private ContentType current;

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
        gameMgr  = new GameManager(transform.Find("Ingame"));
        uiMgr    = new UIManager(transform.Find("UI"));
        sceneMgr = new LevelManager();

        current = ContentType.None;
    }
    private void Start()
    {
        SetContent(ContentType.Opening);
    }
    private void Update()
    {
        inputMgr.Update();
        uiMgr   .Update();
        gameMgr .Update();
    }

    public void SetContent(ContentType type)
    {
        current = type;
        gameMgr.Set(current);
        uiMgr.Set(current);
        inputMgr.Set(gameMgr.GetInputDele(current));
    }
    public void Dispose()
    {
        gameMgr.Dispose(current);
        uiMgr.Dispose(current);
        inputMgr.Set(null);
    }
}