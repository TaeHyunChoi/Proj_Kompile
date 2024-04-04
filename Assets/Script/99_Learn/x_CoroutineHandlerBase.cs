using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class x_CoroutineHandlerBase
{
    protected int index;
    public virtual bool Update()
    {
        return false;  
    }
}

public delegate int x_TestFunc(int index, int count, out int countNext);
public class x_CoroutineHander_1 : x_CoroutineHandlerBase
{
    private x_TestFunc function;
    private int count;

    public x_CoroutineHander_1(x_TestFunc func, int count)
    {
        index = 0;
        this.count = count;
        function = func;
    }
    public override bool Update()
    {
        index = function(index, count, out count);
        return -1 != index;
    }
}
