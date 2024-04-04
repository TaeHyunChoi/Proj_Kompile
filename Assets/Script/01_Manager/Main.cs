using System.Collections;
using UnityEngine;
using System.Threading.Tasks;
using static Public;
using System.Collections.Generic;

public class Main : MonoBehaviour
{
    private static Main  instance;
    private UIManager    mgrUI;
    private InputManager mgrInput;
    private GameManager  mgrGame;
    private LevelManager mgrLevel;

    public static Main          Get { get => instance; }
    public static UIManager     UIMgr    { get => instance.mgrUI; }
    public static InputManager  InputMgr { get => instance.mgrInput; }
    public static GameManager   GameMgr  { get => instance.mgrGame; }
    public static LevelManager  LevelMgr { get => instance.mgrLevel; }
    
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
        mgrLevel = new LevelManager();
    }
    private void Start()
    {
        mgrLevel.LoadOpeningSceneAsync();
        enabled = false;
    }
    private void Update()
    {
        mgrInput.Update();
        mgrGame .Update();
        mgrUI   .Update();
    }
    public Coroutine SetContent(ContentType type)
    {
        current = type;
        IEnumerator ie;
        
        switch (type)
        {
            case ContentType.Opening:   ie = IESetOpening();  break;
            case ContentType.Field:     ie = IESetField();    break;

            default: return null;
        }

        return Coroutiner.PlayCoroutine(ie);
    }

    private IEnumerator IESetOpening()
    {
        // task
        Transform cameraCanvasTransform = mgrUI.GetCameraCanvas().transform;
        Task<OnOpening> task_opening    = OnOpening.InitAsync(cameraCanvasTransform);
        Task<UITitle> task_title        = AssetManager.CreateUIAsync<UITitle>("UITitle", cameraCanvasTransform, false);

        // wait unil task.isDone
        yield return new WaitUntil(() => task_opening.IsCompletedSuccessfully && task_title.IsCompletedSuccessfully);
        yield return new WaitUntil(() => task_title.IsCompletedSuccessfully);

        // content
        OnOpening opening = task_opening.Result;
        mgrGame.SetSequence(opening);

        // ui
        mgrUI.SetBucket((int)UIType.Title, task_title.Result);

        // dispose
        task_opening.Dispose();
        task_title.Dispose();
    }
    private IEnumerator IESetField()
    {
        GameObject mapObj = GameObject.FindWithTag("Field");
        InField field = new InField(mapObj);
        Task<bool> taskInitField = field.InitMap();
        yield return new WaitUntil(() => taskInitField.IsCompletedSuccessfully);

        mgrGame.SetSequence(field);

        taskInitField.Dispose();
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
