namespace Script.Content
{
    using Script.Index;
    using static Script.Index.IDxInput;

    public abstract class _IngameHandlerBase
    {
        protected IngameHandlerType handlerType;
        public IngameHandlerType HandlerType => handlerType;

        public abstract IngameHandlerState MoveNext();
        public abstract void ReceiveInput(InputFlag inputFlag);
        public abstract void Dispose();
    }
}
