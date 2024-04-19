public class CCoroutineHandler
{
    protected int index;
    public virtual bool MoveNext()
    {
        return false;
    }
}

public class CCoroutine<T> : CCoroutineHandler where T: class, IRoutineUpdater
{
    private T routine;

    public CCoroutine(T data)
    {
        this.routine = data;
        index = 0;
    }
    public override bool MoveNext()
    {
        index = routine.MoveNext(index);

        if (-1 == index)
        {
            routine = null;
            return false;
        }

        return true;
    }
}