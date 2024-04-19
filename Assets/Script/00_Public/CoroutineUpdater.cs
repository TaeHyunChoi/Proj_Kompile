using System.Collections.Generic;
using UnityEngine;

public class CoroutineUpdater : MonoBehaviour
{
    private static CoroutineUpdater        instance;
    private static List<CCoroutineHandler> handlers;

    private void Awake()
    {
        if (null != instance)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        handlers = new List<CCoroutineHandler>();
        handlers.Add(null);
    }
    private void Update()
    {
        int index = -1;
        for (int i = 0; i < handlers.Count; ++i)
        {
            if (null == handlers[i])
            {
                continue;
            }
            if (false == handlers[i].MoveNext())
            {
                handlers[i] = null;
                continue;
            }

            index = i;
        }

        //모든 Handler가 null이라면 Update()를 멈춘다.
        if (-1 == index)
        {
            enabled = false;
        }
    }

    //class CoroutineUpdater
    public static void SetHandler(CCoroutineHandler handler)
    {
        if (null == handler)
        {
            UnityEngine.Assertions.Assert.IsNotNull(handler, "Handler is null;");
            return;
        }

        instance.enabled = true;

        //List 중에 빈 자리에 채워 넣는다.
        for (int i = 0; i < handlers.Count; ++i)
        {
            if (null == handlers[i])
            {
                handlers[i] = handler;
                return;
            }
        }

        //빈 자리가 없다면 List에 추가한다.
        handlers.Add(handler);
    }
}

