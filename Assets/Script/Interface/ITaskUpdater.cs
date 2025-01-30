namespace Script.Interface
{
    using Script.Index;

    public interface ITaskUpdater
    {
        public IETaskState MoveNext();
    }
    public interface ITaskInput
    {
        public void InputValue(IDxInput.EInputFlag inputFlag);
    }
}
