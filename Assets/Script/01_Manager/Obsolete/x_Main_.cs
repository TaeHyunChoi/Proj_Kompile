using System.Collections.Generic;
using UnityEngine;
//using static Index.IDxInput;

public partial class x_Main_ : MonoBehaviour
{
    public static x_Main_ Instance { get; private set; }

    private MapSceneManager_ mMapSceneMgr;
    //private UIManager_ mUIMgr;
    // AssetMgr은 staic으로 사용 중;

    private List<IERoutine> mHandlers;

    //private EInputFlag mInputReserved;

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


        /* Initialize Coroutine Updater */
        mHandlers = new List<IERoutine>();


        /* Initialize Managers */
        Transform uiTransform = transform.Find("UI");
        mMapSceneMgr = new MapSceneManager_(uiTransform);
        //mUIMgr       = new UIManager_(uiTransform);


    }
    private void Start()
    {
        mMapSceneMgr.StartGame();
    }

    /* main loop */
    private void Update()
    {
        //mInputReserved = Update_Input();
        Update_Coroutine();
    }
    /*
    private void FixedUpdate()
    {
        var input = mInputReserved;
        switch (mGameState)
        {
            case EGameState.Field:
                // ...
                break;
            default:
                //mInputReserved = input;
                return;
        }
    }
     */
    /*
    private void LateUpdate()
    {
        camera ?
    }     
     */

    /* input */
    //private EInputFlag Update_Input()
    //{
    //    var input = EInputFlag.NONE;

    //    //Button Down
    //    if (Input.GetButtonDown("DOWN"))  { input |= EInputFlag.DOWN; }
    //    if (Input.GetButtonDown("UP"))    { input |= EInputFlag.UP; }
    //    if (Input.GetButtonDown("LEFT"))  { input |= EInputFlag.LEFT; }
    //    if (Input.GetButtonDown("RIGHT")) { input |= EInputFlag.RIGHT; }

    //    if (Input.GetButtonDown("ENTER"))  { input |= EInputFlag.ENTER; }
    //    if (Input.GetButtonDown("CANCEL")) { input |= EInputFlag.CANCEL; }
    //    if (Input.GetButtonDown("ESCAPE")) { input |= EInputFlag.ESCAPE; }
    //    if (Input.GetButtonDown("ACTION")) { input |= EInputFlag.ACTION; }

    //    //Button Hold
    //    if (Input.GetButton("DOWN"))   { input |= EInputFlag.DOWN_HOLD; }
    //    if (Input.GetButton("UP"))     { input |= EInputFlag.UP_HOLD; }
    //    if (Input.GetButton("LEFT"))   { input |= EInputFlag.LEFT_HOLD; }
    //    if (Input.GetButton("RIGHT"))  { input |= EInputFlag.RIGHT_HOLD; }
    //    if (Input.GetButton("ACTION")) { input |= EInputFlag.ACTION_HOLD; }

    //    return input;
    //}

    /* coroutine */
    private void Update_Coroutine()
    {
        for (int i = 0; i < mHandlers.Count; ++i)
        {
            // empty space => continue;
            if (null == mHandlers[i])
            {
                continue;
            }

            // playing => continue;
            if (true == mHandlers[i].MoveNext())
            {
                continue;
            }
            // end => dispose;
            else
            {
                mHandlers[i] = null;
            }
        }
    }
    public void AddCoroutine(IERoutine handler)
    {
        if (null == handler)
        {
            UnityEngine.Assertions.Assert.IsNotNull(handler, "Handler is null;");
            return;
        }

        // Fill in the empty spaces.
        for (int i = 0; i < mHandlers.Count; ++i)
        {
            if (null == mHandlers[i])
            {
                mHandlers[i] = handler;
                return;
            }
        }

        // If there is no empty space, add it to the list.
        mHandlers.Add(handler);
    }
}
