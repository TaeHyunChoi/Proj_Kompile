using System.Collections.Generic;
using UnityEngine;

//쓰읍.. 이거 Main에 걸어버릴까...
public class CoroutineUpdater : MonoBehaviour
{
    //private static CoroutineUpdater        instance;
    //private static List<CCoroutineHandler> mHandlers;

    //private void Awake()
    //{
    //    if (null != instance)
    //    {
    //        Destroy(gameObject);
    //        return;
    //    }
    //    instance = this;
    //    DontDestroyOnLoad(gameObject);

    //    mHandlers = new List<CCoroutineHandler>();
    //    mHandlers.Add(null);
    //}
    //private void Update()
    //{
    //    int index = -1;
    //    for (int i = 0; i < mHandlers.Count; ++i)
    //    {
    //        if (null == mHandlers[i])
    //        {
    //            continue;
    //        }
    //        if (false == mHandlers[i].MoveNext())
    //        {
    //            mHandlers[i] = null;
    //            continue;
    //        }

    //        index = i;
    //    }

    //    //모든 Handler가 null이라면 Update()를 멈춘다.
    //    if (-1 == index)
    //    {
    //        enabled = false;
    //    }
    //}

    //public static void SetHandler(CCoroutineHandler handler)
    //{
    //    if (null == handler)
    //    {
    //        UnityEngine.Assertions.Assert.IsNotNull(handler, "Handler is null;");
    //        return;
    //    }
    //    instance.enabled = true;

    //    //List 중에 빈 자리에 채워 넣는다.
    //    for (int i = 0; i < mHandlers.Count; ++i)
    //    {
    //        if (null == mHandlers[i])
    //        {
    //            mHandlers[i] = handler;
    //            return;
    //        }
    //    }

    //    //빈 자리가 없다면 List에 추가한다.
    //    mHandlers.Add(handler);
    //}
}

