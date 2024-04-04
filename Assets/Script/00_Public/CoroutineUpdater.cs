using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineUpdater : MonoBehaviour
{
    private static CoroutineUpdater instance;

    private List<CoroutineHandler> handlers = new List<CoroutineHandler>();
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

        bool setoff = true;
        for (int i = isFinished.Count - 1; i >= 0; --i)
        {
            if (true == isFinished[i])
            {
                handlers.RemoveAt(i);
            }
            else if (true == setoff)
            {
                setoff = false;
            }
        }

        if (true == setoff)
        {
            enabled = false;
        }
    }
    public static void SetHandler(CoroutineHandler handler)
    {
        if (null == handler)
        {
            Debug.LogError("Handler is null;");
            return;
        }

        if (0 == instance.handlers.Count)
        {
            instance.enabled = true;
        }
        instance.handlers.Add(handler);
    }
}
