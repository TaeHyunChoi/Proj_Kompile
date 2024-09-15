using System;
using System.Collections.Generic;
using UnityEngine;
using static Index.IDxInput;

public partial class Main_ : MonoBehaviour
{
    public static Main_ Instance { get; private set; }

    private MapSceneManager_ mMapSceneMgr;
    private UIManager_ mUIMgr;
    // AssetMgr은 staic으로 사용 중;

    private List<CCoroutineHandler> mHandlers;

    private EInput mInputReserved;

    public EInput InputReserved { get => mInputReserved; }

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
        mHandlers = new List<CCoroutineHandler>();


        /* Initialize Managers */
        Transform uiTransform = transform.Find("UI");
        mMapSceneMgr = new MapSceneManager_(uiTransform);
        mUIMgr       = new UIManager_(uiTransform);


    }
    private void Start()
    {
        mMapSceneMgr.LoadScene_Opening();
    }

    /* main loop */
    private void Update()
    {
        mInputReserved = Update_Input();
        Update_Coroutine();
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
    private void Update_Coroutine()
    {
        int index = -1;
        for (int i = 0; i < mHandlers.Count; ++i)
        {
            if (null == mHandlers[i])
            {
                continue;
            }
            if (false == mHandlers[i].MoveNext())
            {
                mHandlers[i] = null;
                continue;
            }

            index = i;
        }

        if (-1 == index)
        {
            GC.Collect(0, GCCollectionMode.Optimized);
        }
    }

    public void AddCoroutine(CCoroutineHandler handler)
    {
        if (null == handler)
        {
            UnityEngine.Assertions.Assert.IsNotNull(handler, "Handler is null;");
            return;
        }

        //List 중에 빈 자리에 채워 넣는다.
        for (int i = 0; i < mHandlers.Count; ++i)
        {
            if (null == mHandlers[i])
            {
                mHandlers[i] = handler;
                return;
            }
        }

        //빈 자리가 없다면 List에 추가한다.
        mHandlers.Add(handler);
    }
}
