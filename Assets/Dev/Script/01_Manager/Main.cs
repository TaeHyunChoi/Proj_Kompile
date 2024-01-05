using System.Collections;
using UnityEngine;
using System.Threading.Tasks;

public class Main : MonoBehaviour
{
    private static Main         instance;
    private UIManager    uiMgr;
    private InputManager inputMgr;
    private GameManager  gameMgr;
    private LevelManager levelMgr;

    public static Main   Instance { get => instance; }
    public static UIManager     UIMgr    { get => instance.uiMgr; }
    public static InputManager  InputMgr { get => instance.inputMgr; }
    public static GameManager   GameMgr  { get => instance.gameMgr; }
    public static LevelManager  LevelMgr { get => instance.levelMgr; }
    
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
        gameMgr = new GameManager(transform.Find("Ingame"));
        uiMgr = new UIManager(transform.Find("UI"));
        CanvasGroup loadingCurtain = uiMgr.GetOverlayCanvas().transform.GetChild(0).GetComponent<CanvasGroup>();
        levelMgr = new LevelManager(loadingCurtain);

        current = ContentType.None;
    }
    private void Start()
    {
        levelMgr.LoadOpeningSceneAsync();
        enabled = false;
    }
    private void Update()
    {
        inputMgr.Update();
        uiMgr   .Update();
        gameMgr .Update();
    }
    public Coroutine SetContent(ContentType type)
    {
        current = type;
        IEnumerator ie;
        
        switch (type)
        {
            case ContentType.Opening:   ie = SetOpening();  break;
            case ContentType.Field:     ie = SetField();    break;

            default: return null;
        }

        return Coroutiner.PlayCoroutine(ie);
    }
    private IEnumerator SetOpening()
    {
        // task
        Transform cameraCanvasTransform = uiMgr.GetCameraCanvas().transform;
        Task<OnOpening> task_opening = OnOpening.InitAsync(cameraCanvasTransform);
        Task<UITitle> task_title = AssetManager.CreateUIAsync<UITitle>("UITitle", cameraCanvasTransform, false);

        // wait unil task.isDone
        yield return new WaitUntil(() => task_opening.IsCompletedSuccessfully);
        yield return new WaitUntil(() => task_title.IsCompletedSuccessfully);

        // content
        OnOpening opening = task_opening.Result;
        gameMgr.SetSequence(opening);

        // ui
        uiMgr.SetBucket((int)UIType.Title, task_title.Result);

        // dispose
        task_opening.Dispose();
        task_title.Dispose();
    }
    private IEnumerator SetField()
    {
        // init content
        GameObject mapObj = GameObject.FindWithTag("Field");
        InField field = new InField(mapObj);
        Task<bool> initField = field.InitMap();
        yield return new WaitUntil(() => initField.IsCompletedSuccessfully);

        gameMgr.SetSequence(field);

        yield break;
    }
    public void StartContent()
    {
        gameMgr.Start();
        inputMgr.Set(gameMgr.GetInputDele(current));
    }
    public void Dispose()
    {
        gameMgr.Dispose();
        uiMgr.Dispose(current);
    }
}