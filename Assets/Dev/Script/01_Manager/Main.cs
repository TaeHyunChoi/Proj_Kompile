using UnityEngine;

public class Main : MonoBehaviour
{
    private static Main instance;
    public  static Main Instance { get => instance; }

    private IngameContent[] ingame;
    private UIManager ui;

    private int input;
    private int curContent;    //현재 콘텐트 인덱스
    private int prevContent;   //직전 콘텐트 인덱스

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;

        //Assets
        DataTable.LoadTable();

        //Player Data
        //(...)

        //Manager
        ui = UIManager.Instance;
        //(...)

        //Set Content
        ingame = new IngameContent[(int)ContentType.Count];
        ingame[(int)ContentType.Title]  = new OnTitle();
        ingame[(int)ContentType.Field]  = new InField();
        ingame[(int)ContentType.Battle] = new InBattle();

        //Initialize Game (Title로 최초 진입)
        prevContent = (int)ContentType.Count;
        curContent  = (int)ContentType.Title;
        ingame[curContent].Start();
    }
    private void Update()
    {
        //# 입력
        input = 0;
        {
            //현재 방식으로는 키맵핑을 바꿀 수 없구나?

            //Button Down
            if (Input.GetButtonDown("DOWN"))   { input |= IDx.DOWN; }
            if (Input.GetButtonDown("UP"))     { input |= IDx.UP; }
            if (Input.GetButtonDown("LEFT"))   { input |= IDx.LEFT; }
            if (Input.GetButtonDown("RIGHT"))  { input |= IDx.RIGHT; }
            if (Input.GetButtonDown("ENTER"))  { input |= IDx.ENTER; }
            if (Input.GetButtonDown("CANCEL")) { input |= IDx.CANCEL; }
            if (Input.GetButtonDown("ESCAPE")) { input |= IDx.ESCAPE; }
            if (Input.GetButtonDown("ACTION")) { input |= IDx.ACTION; }

            //Button Hold
            if (Input.GetButton("DOWN"))       { input |= IDx.DOWN_HOLD; }
            if (Input.GetButton("UP"))         { input |= IDx.UP_HOLD; }
            if (Input.GetButton("LEFT"))       { input |= IDx.LEFT_HOLD; }
            if (Input.GetButton("RIGHT"))      { input |= IDx.RIGHT_HOLD; }
            if (Input.GetButton("ACTION"))     { input |= IDx.ACTION_HOLD; }
        }

        //## 갱신(처리)
        input         = ui.Update(input);   //ui입력에 성공하면 input = 0; 으로 초기화?
        ingame[curContent].Update(input);   
    }

    public void SetIngame(ContentType idxContentType)
    {
        ingame[curContent].End();          //이전 콘텐트 종료
        prevContent = curContent;          //현재 콘텐트 인덱스를 직전 콘텐트 인덱스로 저장
        curContent = (int)idxContentType;  //콘텐트 번호 갱신
        ingame[curContent].Start();        //신규 콘텐트 시작
    }
}