namespace Script.Content
{
    using Script.Index;
    using Script.Interface;
    using Script.Manager;
    using System.Collections.Generic;
    using static Script.Index.IDxInput;

    public abstract class _IngameHandlerBase
    {
        protected IngameHandlerType handlerType;
        protected List<IngameAsset_t> assets;

        public IngameHandlerType HandlerType => handlerType;

        protected abstract void ExecuteIngameEventAsync(IngameEventType messageType);
        public abstract void ReceiveIngameInput(InputFlag inputFlag);

        /// <summary> 신경 안쓰고 싶어서 virtual로 일괄 Dispose <br/>
        /// 필요하면 override 하여 기능 추가 </summary>
        public virtual void Dispose()
        {
            for (int i = 0; i < assets.Count; ++i)
            {
                assets[i].Dispose();
            }

            if (this is IMessageReceiver receiver)
            {
                MessageManager.Dispose(receiver);
            }
        }

        public _IngameHandlerBase()
        {
            assets = new List<IngameAsset_t>();
        }
    }
}
