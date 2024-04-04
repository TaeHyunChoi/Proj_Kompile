using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCoroutineHandler
{
    protected int index;
    public virtual bool MoveNext()
    {
        return false;
    }
}
public class CCoroutine<T> : CCoroutineHandler where T: class, IUpdateRoutine
{
    private T routine;

    public CCoroutine(T data)
    {
        this.routine = data;
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