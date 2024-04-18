using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineUpdater : MonoBehaviour
{
    private static CoroutineUpdater instance;   //singleton
    private List<CCoroutineHandler> handlers = new List<CCoroutineHandler>();

    private List<bool> isFinished = new List<bool>();

    private void Awake()
    {
        if (null != instance)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void Update()
    {
        isFinished.Clear();
        for (int i = 0; i < handlers.Count; ++i)
        {
            isFinished.Add(!handlers[i].MoveNext());
        }
        if (0 == isFinished.Count)
        {
            enabled = false;
            return;
        }

        for (int i = isFinished.Count - 1; i >= 0; --i)
        {
            if (true == isFinished[i])
            {
                handlers.RemoveAt(i);
            }
        }
    }

    public static void SetHandler(CCoroutineHandler handler)
    {
        if (null == handler)
        {
#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
            Debug.LogError("Handler is null;");
#endif
            return;
        }

        instance.handlers.Add(handler);
        if (1 == instance.handlers.Count)
        {
            instance.enabled = true;
        }
    }
}