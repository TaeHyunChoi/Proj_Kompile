using Kompile.Data;
using System.Collections.Generic;

namespace Kompile.Manager
{
    public abstract class GameLogicMgrBase
    {
        protected List<RequestBase> _inbox      = new List<RequestBase>(64);
        protected List<RequestBase> _processing = new List<RequestBase>(64);
        
        public void Enqueue(RequestBase request)
        {
            _inbox.Add(request);
        }

        public abstract void OnUpdate();
    }
}
