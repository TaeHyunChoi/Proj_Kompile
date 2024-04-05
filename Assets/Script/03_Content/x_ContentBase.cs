using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class x_ContentOpening : MonoBehaviour
{
    protected int state;
    protected bool canSkip;
    public abstract void Begin();
    public abstract void Next();
    public void NextState()
    {
        if (canSkip)
        {
            ++state;
            canSkip = false;
        }
    }

    public abstract void Playing();
}
