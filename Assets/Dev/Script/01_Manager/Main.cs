using UnityEngine;
using System.Collections;
using System.Threading.Tasks;

public class Main : MonoBehaviour
{
    private static Main instance;

    private ContentBase[] ingame;
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

        //Play Opening
        OnOpening opening = new OnOpening();
        Task<bool> isOpen = opening.InitAsync(canvas_overlay.transform);

        if (isOpen.Result)
        {
            DataTable.LoadTable();
            //Load Player Data
        }
        else
        {
            Debug.LogError("$$ Fail to load Opening.$$ ");
            Application.Quit();
            return;
        }

        //Init Content
        ingame = new ContentBase[(int)ContentType.Count];
        ingame[(int)ContentType.Title]   = new OnTitle();
        ingame[(int)ContentType.Field]   = new InField();
        ingame[(int)ContentType.Battle]  = new InBattle();
        prevContent = (int)ContentType.Title;
    }
    private void Start()
    {
        Debug.Log("Start");
    }
    //private void Update()
    //{
    //    inputMgr.Update();
    //    ingame[curContent].Update();
    //}

    public void SetIngame(ContentType contentType)
    {
        ingame[curContent].End();          //이전 콘텐트 종료

        prevContent = curContent;          //현재 콘텐트 인덱스를 직전 콘텐트 인덱스로 저장
        curContent = (int)contentType;     //콘텐트 번호 갱신

        //uiMgr.Set(contentType); 
        inputMgr.Set(ingame[curContent].InputEvent);

        ingame[curContent].Start();        //신규 콘텐트 시작
    }
}