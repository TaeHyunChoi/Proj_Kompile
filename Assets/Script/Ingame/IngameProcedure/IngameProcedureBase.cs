namespace Script.Content
{
    using Script.Index;
    using Script.Interface;
    using Script.Manager;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class IngameProcedureBase
    {
        protected IngameProcedureType procedureType;
        protected List<(AssetCode code, GameObject obj)> ingameObjects;

        public IngameProcedureType HandlerType => procedureType;

        protected abstract void ExecuteIngameEventAsync(IngameEventType messageType);

        /// <summary> 신경 안쓰고 싶어서 virtual로 일괄 Dispose <br/>
        /// 필요하면 override 하여 기능 추가 </summary>
        public virtual void Dispose()
        {
            if (this is IMessageReceiver receiver)
            {
                MessageManager.Dispose(receiver);
            }

            for (int i = 0; i < ingameObjects.Count; ++i)
            {
                AssetManager.ReleaseInstance(ingameObjects[i].code, ingameObjects[i].obj);
            }
        }

        protected async Task<T> CreateIngameObjectAsync<T>(AssetCode assetCode) where T:IngameMonoBehaviourBase
        {
            GameObject obj = await AssetManager.CreateInstanceAsync(assetCode);
            ingameObjects.Add(new(assetCode, obj));

            return obj.GetComponent<T>();
        }

        public IngameProcedureBase()
        {
            if (this is IMessageReceiver receiver)
            {
                MessageManager.AddReceiver(receiver);
            }

            ingameObjects = new List<(AssetCode, GameObject)>();
        }
    }
}