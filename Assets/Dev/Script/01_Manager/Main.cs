using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using UnityEditor;
using System.Threading;

public class Main : MonoBehaviour
{
    private static Main main;

    private UIManager     uiMgr;
    private InputManager  inputMgr;

    private Canvas canvas_overlay;
    private Canvas canvas_camera;

    private void Awake()
    {
        //Singleton
        if (main != null)
        {
            Destroy(this.gameObject);
            return;
        }
        main = this;

        Init();
        main.enabled = false;
    }
    private void Init()
    {
        //Init Manager
        uiMgr = new UIManager(transform.Find("UI"));
        canvas_overlay = uiMgr.GetTransform().GetChild(0).GetComponent<Canvas>();
        canvas_camera = uiMgr.GetTransform().GetChild(1).GetComponent<Canvas>();

        inputMgr = new InputManager();
        inputMgr.Set(ContentType.Opening);

        DataTable.LoadTable();
        //+ Load Player Data

        this.StartCoroutine(IEOpeningAsync());
    }
    private IEnumerator IEOpeningAsync()
    {
        //생성 순서를 확정하고자 동기화 느낌으로.
        Task<OnOpening> taskOpening = OnOpening.InitAsync(canvas_overlay.transform);
        yield return new WaitUntil(() => taskOpening.IsCompleted);

        Task<OnTitle> taskTitle = OnTitle.InitAsync(canvas_overlay.transform);
        yield return new WaitUntil(() => taskTitle.IsCompleted);

        taskOpening.Dispose();
        taskTitle.Dispose();

        inputMgr.Set(ContentType.Opening);
        main.enabled = true;
    }

    private void Update()
    {
        inputMgr.Update();
    }
}