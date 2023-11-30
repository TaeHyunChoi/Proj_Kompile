using UnityEngine;

public class DMain : MonoBehaviour
{
    private static DMain instance;
    public  static DMain Instance { get => instance; }

    private DIngameContent[] ingame;
    private DUIManager ui;

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
        DDataTable.LoadTable();

        //Player Data
        //(...)

        //Manager
        ui = DUIManager.Instance;
        //(...)

        //Set Content
        ingame = new DIngameContent[(int)ContentType.Count];
        ingame[(int)ContentType.Title]  = new DIngameTitle();
        ingame[(int)ContentType.Field]  = new DIngameField();
        ingame[(int)ContentType.Battle] = new DIngameBattle();

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
            if (Input.GetButtonDown("DOWN"))   { input |= DIDx.DOWN; }
            if (Input.GetButtonDown("UP"))     { input |= DIDx.UP; }
            if (Input.GetButtonDown("LEFT"))   { input |= DIDx.LEFT; }
            if (Input.GetButtonDown("RIGHT"))  { input |= DIDx.RIGHT; }
            if (Input.GetButtonDown("ENTER"))  { input |= DIDx.ENTER; }
            if (Input.GetButtonDown("CANCEL")) { input |= DIDx.CANCEL; }
            if (Input.GetButtonDown("ESCAPE")) { input |= DIDx.ESCAPE; }
            if (Input.GetButtonDown("ACTION")) { input |= DIDx.ACTION; }

            //Button Hold
            if (Input.GetButton("DOWN"))       { input |= DIDx.DOWN_HOLD; }
            if (Input.GetButton("UP"))         { input |= DIDx.UP_HOLD; }
            if (Input.GetButton("LEFT"))       { input |= DIDx.LEFT_HOLD; }
            if (Input.GetButton("RIGHT"))      { input |= DIDx.RIGHT_HOLD; }
            if (Input.GetButton("ACTION"))     { input |= DIDx.ACTION_HOLD; }
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