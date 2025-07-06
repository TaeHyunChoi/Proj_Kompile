namespace Script.Content
{
    using Script.Index;
    using Script.Interface;
    using Script.Manager;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class _IngameHandlerBase
    {
        protected IngameHandlerType handlerType;
        protected List<IngameAsset_t> assets;

        protected List<(AssetCode code, GameObject obj)> asset_codes;

        public IngameHandlerType HandlerType => handlerType;

        protected abstract void ExecuteIngameEventAsync(IngameEventType messageType);

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

            for (int i = 0; i < asset_codes.Count; ++i)
            {
                AssetManager.ReleaseInstance(asset_codes[i].code, asset_codes[i].obj);
            }
        }

        public _IngameHandlerBase()
        {
            assets = new List<IngameAsset_t>();

            if (this is IMessageReceiver receiver)
            {
                MessageManager.AddReceiver(receiver);
            }

            asset_codes = new List<(AssetCode, GameObject)>();
        }
        //~_IngameHandlerBase()
        //{
        //    if (this is IMessageReceiver receiver)
        //    {
        //        MessageManager.Dispose(receiver);
        //    }
        //}
    }
}