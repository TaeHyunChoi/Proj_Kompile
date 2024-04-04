using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineUpdater : MonoBehaviour
{
    private static CoroutineUpdater instance;
    public static CoroutineUpdater Get { get => instance; }

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
    public void SetHandler(CCoroutineHandler handler)
    {
        if (null == handler)
        {
            Debug.LogError("Handler is null;");
            return;
        }

        if (0 == handlers.Count)
        {
            enabled = true;
        }
        handlers.Add(handler);
    }
}
