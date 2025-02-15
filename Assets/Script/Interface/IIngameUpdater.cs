namespace Script.Interface
{
    using Script.Index;

    public interface IIngameUpdater
    {
        public UpdaterState UpdateState();
    }
    public interface IIngameFixedUpdater
    {
        public UpdaterState FixedUpdateState();
    }
    public interface IIngameLateUpdater
    {
        public UpdaterState LateUpdateState();
    }

    public interface IIngameInput
    {
        public void Input(IDxInput.EInputFlag inputFlag);
    }
}
