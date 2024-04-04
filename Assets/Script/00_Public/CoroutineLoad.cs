using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineHandler
{
    protected int index;
    public virtual bool MoveNext()
    {
        return false;
    }
}
public class CoroutineLoad<T> : CoroutineHandler where T: class, IUpdateRoutine
{
    private T routine;

    public CoroutineLoad(T level)
    {
        this.routine = level;
        index = 0;
    }
    public override bool MoveNext()
    {
        index = routine.Update(index);

        if (-1 == index)
        {
            routine = null;
            return false;
        }

        //possible to move next;
        return true;
    }
}