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
            InDev.Log($"{GetType()}.Enqueue Request (count: {_inbox.Count})");
        }

        public abstract void RegisterToCache();
        public abstract Awaitable<bool> OnAwake();
        public abstract Awaitable<bool> OnUpdate();
        public abstract void OnDisable();

        protected abstract Awaitable<bool> HandleRequestAsync(RequestBase request);
        protected async Awaitable<bool> ProcessRequests() 
        {
            if (_inbox.Count == 0)
            {
                return false;
            }

            _swap = _inbox;
            _inbox = _processing;
            _processing = _swap;

            bool progress = true;
            for (int i = 0; i < _swap.Count; ++i)
            {
                RequestBase request = _processing[i];
                bool success = await HandleRequestAsync(request);
                
                // 오류가 발생해도 계속 루프 돌리는 것으로;
                progress &= success;
                
#if DEV_BUILD
                if (!success)
                {
                    InDev.LogError($"Can`t Handle Request: {request.GetType()}");
                }
#endif
            }

            _swap = null;
            return progress;
        }
    }
}
