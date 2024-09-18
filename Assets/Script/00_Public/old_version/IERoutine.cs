public class IERoutineHandler
{
    protected int mhIndex;
    public bool IsDone { get => -1 == mhIndex; }

    public IERoutineHandler()
    {
        mhIndex = 0;
    }
    public virtual bool MoveNext()
    {
        return false;
    }
}
public class IERoutine : IERoutineHandler
{
    private readonly IRoutineUpdater[] mRoutineArray;
    private readonly int               mLength;

    private int mArrayIndex;


    public IERoutine(params IRoutineUpdater[] routines) : base()
    {
        mRoutineArray  = routines;
        mLength = routines.Length;
        mArrayIndex = 0;
    }

    public override bool MoveNext()
    {
        base.mhIndex = mRoutineArray[mArrayIndex].MoveNext(base.mhIndex);

        if (-1 == base.mhIndex)
        {
            bool isNext = ++mArrayIndex < mLength;
            if (true == isNext)
            {
                base.mhIndex = 0;
            }

            return isNext;
        }

        return true;
    }
}