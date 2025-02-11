namespace Script.Interface
{
    using Script.Index;

    public interface ITaskUpdater
    {
        public UpdaterState MoveNext();
    }
}
