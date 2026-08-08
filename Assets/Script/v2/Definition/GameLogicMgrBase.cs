namespace Kompile.Manager
{
    using Data;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class GameLogicMgrBase
    {
        public float Prior { get; protected set; }
        protected List<RequestBase> _inbox      = new List<RequestBase>(64);
        protected List<RequestBase> _processing = new List<RequestBase>(64);
        protected List<RequestBase> _swap;

        public void Enqueue(RequestBase request)
        {
            _inbox.Add(request);
        }

        public abstract void RegisterToCache();
        public abstract Awaitable<bool> OnAwake();
        public abstract void OnUpdate();
        public abstract void OnDisable();

        protected void ProcessRequests() 
        {
            if (_inbox.Count == 0)
            {
                return;
            }

            _swap = _inbox;
            _inbox = _processing;
            _processing = _swap;

            for (int i = 0; i < _swap.Count; ++i)
            {
                RequestBase request = _processing[i];
                InGameLogic.Progress(request);
            }

            _swap = null;
        }
    }
}
