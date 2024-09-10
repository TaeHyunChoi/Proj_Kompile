using UnityEngine;
using static Index.IDxInput;

public partial class Main_ : MonoBehaviour
{
    public static Main_ Instance { get; private set; }

    private MapSceneManager_ mMapSceneMgr;
    private UIManager_ mUIMgr;
    private AssetManager_ mAssetMgr;


    private EInput mInputReserved;
    private EGameState mGameState;

    private void Awake()
    {
        /* like singleton */
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        /* Initialize Managers */
        mMapSceneMgr = new MapSceneManager_(Instance.transform);
        mUIMgr       = new UIManager_(Instance.transform);
        mAssetMgr    = new AssetManager_();
    }
    private void Start()
    {
        mMapSceneMgr.LoadScene_Opening();
    }

    /* main loop */
    private void Update()
    {
        var input = Update_Input();
        switch (mGameState)
        {
            case EGameState.Opening: mMapSceneMgr.InputOpening(input); break;
            default:
                mInputReserved = input;
                return;
        }
    }
    //private void FixedUpdate()
    //{
    //    var input = mInputReserved;
    //    switch (mGameState)
    //    {
    //        case EGameState.Field:
    //            // ...
    //            break;
    //        default:
    //            //mInputReserved = input;
    //            return;
    //    }
    //}
    //private void LateUpdate()
    //{
    //      camera?
    //}

    private EInput Update_Input()
    {
        var input = EInput.NONE;

        //Button Down
        if (Input.GetButtonDown("DOWN"))  { input |= EInput.DOWN; }
        if (Input.GetButtonDown("UP"))    { input |= EInput.UP; }
        if (Input.GetButtonDown("LEFT"))  { input |= EInput.LEFT; }
        if (Input.GetButtonDown("RIGHT")) { input |= EInput.RIGHT; }

        if (Input.GetButtonDown("ENTER"))  { input |= EInput.ENTER; }
        if (Input.GetButtonDown("CANCEL")) { input |= EInput.CANCEL; }
        if (Input.GetButtonDown("ESCAPE")) { input |= EInput.ESCAPE; }
        if (Input.GetButtonDown("ACTION")) { input |= EInput.ACTION; }

        //Button Hold
        if (Input.GetButton("DOWN"))   { input |= EInput.DOWN_HOLD; }
        if (Input.GetButton("UP"))     { input |= EInput.UP_HOLD; }
        if (Input.GetButton("LEFT"))   { input |= EInput.LEFT_HOLD; }
        if (Input.GetButton("RIGHT"))  { input |= EInput.RIGHT_HOLD; }
        if (Input.GetButton("ACTION")) { input |= EInput.ACTION_HOLD; }

        return input;
    }
}
