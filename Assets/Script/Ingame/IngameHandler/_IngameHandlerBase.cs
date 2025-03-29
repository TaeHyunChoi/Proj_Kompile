namespace Script.Content
{
    using Script.Index;

    public abstract class _IngameHandlerBase
    {
        protected IngameHandlerType handlerType;

        public IngameHandlerType HandlerType => handlerType;
        public abstract IngameHandlerState MoveNext();
        public abstract void Dispose();
    }
}
