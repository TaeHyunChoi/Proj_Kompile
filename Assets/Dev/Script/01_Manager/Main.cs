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

    private int curContent;    //ÇöÀç ÄÜÅÙÆ® ÀÎµ¦½º
    private int prevContent;   //Á÷Àü ÄÜÅÙÆ® ÀÎµ¦½º

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

        DataTable.LoadTable();
        //+ Load Player Data

        //Init Content
        prevContent = (int)ContentType.Title;
    }
    private void Start()
    {
        this.StartCoroutine(IEOpeningAsync());
    }
    private IEnumerator IEOpeningAsync()
    {
        Task<bool> taskOpening = OnOpening.InitAsync(canvas_overlay.transform);
        Task<bool> taskTitle = OnTitle.InitAsync(canvas_overlay.transform);

        while (!taskOpening.IsCompleted || !taskTitle.IsCompleted)
        {
            yield return null;
        }

        taskOpening.Dispose();
        taskTitle.Dispose();
    }

    //private void Update()
    //{
    //    inputMgr.Update();
    //    ingame[curContent].Update();
    //}
}