namespace Script.Interface
{
    using Script.Index;

    public interface IIngameUpdater
    {
        public IngameUpdateState UpdateState();
    }
    public interface IIngameFixedUpdater
    {
        public IngameUpdateState FixedUpdateState();
    }
    public interface IIngameLateUpdater
    {
        public IngameUpdateState LateUpdateState();
    }
}
