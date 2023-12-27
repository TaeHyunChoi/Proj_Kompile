using UnityEngine;
using System.Collections;

public class Main : MonoBehaviour
{
    private static Main instance;

    private static UIManager    uiMgr;
    private static InputManager inputMgr;
    private static GameManager  gameMgr;

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

        uiMgr     = new UIManager(transform.Find("UI"));
        inputMgr  = new InputManager();
        gameMgr = transform.GetComponentInChildren<GameManager>();
    }
    private void Start()
    {
        gameMgr.InitContent(ContentType.Opening);
    }
    private void Update()
    {
        inputMgr.Update();
    }

    public static Main GetInstance()
    {
        return instance;
    }
    public static UIManager GetUIManager()
    {
        return uiMgr;
    }
    public static InputManager GetInputManager()
    {
        return inputMgr;
    }
    public static GameManager GetGameManager()
    {
        return gameMgr;
    }
}