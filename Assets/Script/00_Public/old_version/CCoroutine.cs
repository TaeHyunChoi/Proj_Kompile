public class CCoroutineHandler
{
    protected int mindex;
    public bool IsDone { get => -1 == mindex; }

    public virtual bool MoveNext()
    {
        return false;
    }
}
public class CCoroutine<T> : CCoroutineHandler where T : class, IRoutineUpdater
{
    private T mRoutine;

    public CCoroutine(T coroutine)
    {
        mRoutine = coroutine;
        mindex = 0;
    }
    public override bool MoveNext()
    {
        mindex = mRoutine.MoveNext(mindex);

        if (-1 == mindex)
        {
            mRoutine = null;
            return false;
        }

        return true;
    }
}