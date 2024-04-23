using UnityEngine;

public class Main : MonoBehaviour
{
    //TODO: FrameRate는 추후에 Config.cs 등에게 넘기기
    [SerializeField]
    private int mFrameRate = 144; 

    public static Main          Instance { get; private set; }
    public static UnitPlayer    Player   { get; set; }
    public static InputMgr      InputMgr { get; private set; }
    public static SceneMgr      SceneMgr { get; private set; }
    public static UnitMgr       UnitMgr  { get; private set; }
    public static UIMgr         UIMgr    { get; private set; }
    public static CameraFollow  Cam      { get; private set; }

    private ContentBase mContent;

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
    float deltaTime = 0.0f;
#endif

    private void Awake()
    {
        //like Singleton
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        DataTable.LoadTable();
        //TODO: Load Player Saved Data

        InputMgr = transform.GetComponent<InputMgr>();
        Cam      = Camera.main.transform.GetComponent<CameraFollow>();
        UIMgr    = new UIMgr   (transform.Find("UI"));
        UnitMgr  = new UnitMgr (transform.Find("Unit"));
        SceneMgr = new SceneMgr(transform.Find("Scene"));
    }
    private void Start()
    {
        SceneMgr.LoadSceneAsync(EGameStateFlag.Opening);
        Application.targetFrameRate = mFrameRate;
    }
    private void Update()
    {
        UnitMgr.Update();

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
#endif
    }

    // set func()
    public void SetContent(ContentBase content)
    {
        mContent = content;
        InputMgr.Updater      = content as IInputHandler;
        InputMgr.FixedUpdater = content as IFixedInputHandler;
    }
    public void SetFieldLayer(int layer)
    {
        OnField field = mContent as OnField;
        UnityEngine.Assertions.Assert.IsNotNull(field, "field is null;");
        field.TransLayer(layer);
    }
    public void SetPlayer(UnitPlayer player)
    {
        UnityEngine.Assertions.Assert.IsNotNull(player, "null player;");
        Player = player;
        Cam.SetFollow(player.Transform);
    }

    public void Release()
    {
        if (null != mContent)
        {
            mContent.Release();
        }

        UIMgr.Release();
    }

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
    private void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 50;
        style.normal.textColor = new Color(0f, 1f, 0f, 1f);
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        GUI.Label(rect, text, style);
    }
#endif
}
