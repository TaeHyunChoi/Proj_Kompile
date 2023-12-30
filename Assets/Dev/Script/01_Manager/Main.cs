using UnityEngine;

public class Main : MonoBehaviour
{
    private static Main instance;

    private static UIManager    uiMgr;
    private static InputManager inputMgr;
    private static GameManager  gameMgr;

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
        gameMgr  = new GameManager(transform.Find("Ingame"));
        uiMgr    = new UIManager(transform.Find("UI"));
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
    

    private void SetContent(ContentType type)
    {
        DisposePrev();

        //순서 엄수. (game -> ui -> input)
        gameMgr. Set(type);
        uiMgr.   Set(type);
        inputMgr.Set(gameMgr.GetInputDele(type));
    }
    private void DisposePrev()
    {
        Debug.Log("Dispose Prev Content.");
    }
}