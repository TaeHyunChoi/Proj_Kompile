namespace Script.Content
{
    using Script.Interface;
    using System.Collections.Generic;
    using System.Threading;
    using UnityEngine;

    public abstract class ContentBase : IContentUpdater
    {
        protected List<IngameMonoBehaviourBase> child_instance = new List<IngameMonoBehaviourBase>();
        protected List<IContentUpdater>         child_updater  = new List<IContentUpdater>();
        protected CancellationTokenSource       skipToken;

        public abstract Awaitable EnterAync();
        public abstract void Exit();
        public void OnUpdate()
        {
            for (int i = child_updater.Count - 1; i >= 0; --i)
            {
                child_updater[i].OnUpdate();
            }
        }
        protected CancellationToken RefreshSkipToken()
        {
            if (null != skipToken)
            {
                skipToken?.Dispose();
            }
            skipToken = new CancellationTokenSource();
            return skipToken.Token;
        }

        ~ContentBase()
        {
            for (int i = child_updater.Count; i >= 0; --i)
            {

            }
        }
    }
}