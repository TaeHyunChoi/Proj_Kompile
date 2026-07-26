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
        
        public void Enqueue(RequestBase request)
        {
            _inbox.Add(request);
        }

        public abstract Awaitable<bool> OnAwake();
        public abstract void OnUpdate();
        public abstract void OnDisable();
    }
}
