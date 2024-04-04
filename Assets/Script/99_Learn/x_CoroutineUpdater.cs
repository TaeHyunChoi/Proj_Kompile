using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class x_CoroutineUpdater : MonoBehaviour
{
    private List<x_CoroutineHandlerBase> handlers = new List<x_CoroutineHandlerBase>();
    private List<bool> finished = new List<bool>();

    private void Awake()
    {
        handlers.Add(new x_CoroutineHander_1(new x_TestFunc(Func01), 0));
        //finished.Add(false);
        handlers.Add(new x_CoroutineHander_1(new x_TestFunc(Func02), 0));
        //finished.Add(false);
    }
    private void Update()
    {
        finished.Clear();
        for (int i = 0; i < handlers.Count; ++i)
        {
            finished.Add(!handlers[i].Update());
        }

        bool setoff = true;
        for (int i = finished.Count - 1; i >= 0; --i)
        {
            if (true == finished[i])
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

    private int Func01(int index, int count, out int countNext)
    {
        countNext = 0;
        switch (index)
        {
            case 0:
                Debug.Log("Call Func01");
                return index += 1;
            case 1:
                if (count < 10)
                {
                    countNext = count + 1;
                    Debug.Log(countNext + " = Func01");
                    return index;
                } 
                return index += 1;
            default:
                break;
        }

        countNext = 10;
        Debug.Log("End Func01");
        return -1;
    }
    private int Func02(int index, int count, out int countNext)
    {
        countNext = 0;
        switch (index)
        {
            case 0:
                Debug.Log("Call Func02");
                return index += 1;
            case 1:
                if (count < 10)
                {
                    countNext = count + 1;
                    Debug.Log(countNext + " = Func02");
                    return index;
                }
                return index += 1;
            default:
                break;
        }

        countNext = 10;
        Debug.Log("End Func02");
        return -1;
    }

}
