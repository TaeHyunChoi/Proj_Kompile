using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using UnityEditor;
using System.Threading;

public class Main : MonoBehaviour
{
    private static Main instance;

    private UIManager     uiMgr;
    private InputManager  inputMgr;

    private Canvas canvas_overlay;
    private Canvas canvas_camera;

    private int curContent;    //현재 콘텐트 인덱스
    private int prevContent;   //직전 콘텐트 인덱스

    private void Awake()
    {
        //Singleton
        if (instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;

        //Init Manager
        uiMgr = new UIManager(transform.Find("UI"));
        {
            canvas_overlay = uiMgr.GetTransform().GetChild(0).GetComponent<Canvas>();
            canvas_camera = uiMgr.GetTransform().GetChild(1).GetComponent<Canvas>();
        }
        inputMgr = new InputManager();
        inputMgr.Set(ContentType.Count);

        DataTable.LoadTable();
        //+ Load Player Data

        //Init Content
        prevContent = (int)ContentType.Title;
    }
    private void Start()
    {
        this.StartCoroutine(IEOpeningAsync());
    }
    private void Update()
    {
        inputMgr.Update();
    }
    private IEnumerator IEOpeningAsync()
    {
        //생성 순서를 확정하고자 동기화 느낌으로.
        Task<bool> taskOpening = OnOpening.InitAsync(canvas_overlay.transform);
        yield return new WaitUntil(() => taskOpening.IsCompleted);
        taskOpening.Dispose();

        inputMgr.Set(ContentType.Title);

        Task<bool> taskTitle = OnTitle.InitAsync(canvas_overlay.transform);
        yield return new WaitUntil(() => taskTitle.IsCompleted);
        taskTitle.Dispose();
    }

    //private void Update()
    //{
    //    inputMgr.Update();
    //    ingame[curContent].Update();
    //}
}