using UnityEngine;
using System.Collections;

public class Main : MonoBehaviour
{
    private static Main instance;

    private static UIManager    uiMgr;
    private static InputManager inputMgr;
    private static GameManager  gameMgr;

    //테스트 중이니까 일단 나열
    private bool[] enable;
    //해보고 괜찮으면 enum 등으로 함 써봅시다.
    private readonly int idxInput = 0;
    private readonly int idxUI    = 1;
    private readonly int idxGame  = 2;
    private int prev, current;


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
        DontDestroyOnLoad(gameObject);

        DataTable.LoadTable();
        //++Load Player Data

        inputMgr = new InputManager();
        uiMgr    = new UIManager(transform.Find("UI"));
        gameMgr  = new GameManager(transform.Find("Ingame"));

        enable  = new bool[3] { false, false, false };
        current = 0;
    }
    private void Start()
    {
        gameMgr.InitContent(ContentType.Opening);
    }
    private void Update()
    {
        if (enable[0]) { inputMgr.Update(); }
        if (enable[1]) { uiMgr.Update();    }
        if (enable[2]) { gameMgr.Update();  }
    }
    public static void OnEnable(int index, InputDele inputFunc)
    {
        instance.enable[instance.current] = false;  //이전 레이어 비활성화
        instance.prev = instance.current;
        instance.enable[index] = true;              //선택 레이어   활성화

        inputMgr.SetInputDele(inputFunc);           //선택 레이어  세팅
        instance.enable[instance.idxInput] = true;  //입력 레이어  활성화
        instance.current = index;                   //선택 레이어  갱신
    }
    public static void OnDis_Enable()
    {
        instance.enable[instance.current] = false; //이러면 UI 여러 레이어일 때가 안되는건가?
    }
}