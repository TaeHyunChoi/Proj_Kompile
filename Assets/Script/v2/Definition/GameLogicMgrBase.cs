using Kompile.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Kompile.Manager
{
    public abstract class GameLogicMgrBase
    {
        public int Prior { get; protected set; }
        protected List<RequestBase> _inbox      = new List<RequestBase>(64);
        protected List<RequestBase> _processing = new List<RequestBase>(64);
        
        public void Enqueue(RequestBase request)
        {
            _inbox.Add(request);
        }

        public abstract Awaitable<bool> OnAwake();
        public abstract void OnUpdate();
    }
}
