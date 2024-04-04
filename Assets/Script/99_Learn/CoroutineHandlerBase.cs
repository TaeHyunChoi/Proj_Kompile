using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineHandlerBase
{
    protected int index;
    public virtual bool Update()
    {
        return false;  
    }
}

public delegate int TestFunc(int index, int count, out int countNext);
public class CoroutineHander_1 : CoroutineHandlerBase
{
    private TestFunc function;
    private int count;

    public CoroutineHander_1(TestFunc func, int count)
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
