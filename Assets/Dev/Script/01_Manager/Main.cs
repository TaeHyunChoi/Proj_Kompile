using UnityEngine;
using System.Collections;

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

        DataTable.LoadTable();
        //++Load Player Data

        uiMgr    = new UIManager(transform.Find("UI"));
        inputMgr = new InputManager();
        gameMgr  = transform.GetComponentInChildren<GameManager>();
    }
    private void Start()
    {
        gameMgr.InitContent(ContentType.Opening);
    }
    private void Update()
    {
        inputMgr.Update();
    }


    public static void SetCurrentUI(UIType type)
    {
        uiMgr.   SetCurrentUI(type, out InputDele func);
        inputMgr.SetInputFunc(func);
    }
}