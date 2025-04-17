namespace Script.Content
{
    using Script.Index;
    using static Script.Index.IDxInput;

    public abstract class _IngameHandlerBase
    {
        protected IngameHandlerType handlerType;
        public IngameHandlerType HandlerType => handlerType;

        public abstract void ExecuteIngameEventAsync(IngameEventType messageType);
        public abstract void Receive_Input(InputFlag inputFlag);
        public abstract void Dispose();
    }
}
