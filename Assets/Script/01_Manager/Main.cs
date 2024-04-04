using System.Collections;
using UnityEngine;
using System.Threading.Tasks;
using static Public;
using System.Collections.Generic;

public partial class Main : MonoBehaviour
{
    private static Main  instance;
    private UIManager    mgrUI;
    private InputManager mgrInput;
    private GameManager  mgrGame;
    private SceneManager mgrScene;

    public static Main          Get { get => instance; }
    public static UIManager     UIMgr    { get => instance.mgrUI; }
    public static InputManager  InputMgr { get => instance.mgrInput; }
    public static GameManager   GameMgr  { get => instance.mgrGame; }
    public static SceneManager  SceneMgr { get => instance.mgrScene; }
    
    private ContentType current;

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

        current = ContentType.None;

        mgrInput = new InputManager();
        mgrGame  = new GameManager(transform.Find("Ingame"));
        mgrUI    = new UIManager(transform.Find("UI"));
        mgrScene = new SceneManager();
    }
    private void Start()
    {
        mgrScene.LoadSceneAsync(ContentType.Opening, -1);
        enabled = false;
    }
    private void Update()
    {
        mgrInput.Update();
        mgrGame .Update();
        mgrUI   .Update();
    }

    public void StartContent()
    {
        mgrGame.Start();
        mgrInput.Set(mgrGame.GetInputDele(current));
    }
    public void Dispose()
    {
        mgrGame.Dispose();
        mgrUI.Dispose(current);
    }
}
