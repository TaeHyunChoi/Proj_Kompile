using System.Collections.Generic;
using UnityEngine;
using Kompile.Data;
using Kompile.Provider;

namespace Kompile.Manager
{
    public class CharacterMgr : GameLogicMgrBase
    {
#pragma warning disable 1998
        public override async Awaitable<bool> OnAwake()
        {
            Prior = 1;
            return true;
        }
#pragma warning restore 1998
        public override void OnUpdate()
        {
            if (_inbox.Count == 0)
            {
                return;
            }

            List<RequestBase> swap = _inbox;
            _inbox = _processing;
            _processing = swap;

            for (int i = 0; i < swap.Count; ++i)
            {
                RequestBase req = _processing[i];

                switch (req.Type)
                {
                    case RequestType.PlayerMove:
                        PlayerMoveRequest moveRq = (PlayerMoveRequest)req;
                        Vector3 input = moveRq.InputVector;

                        // 플레이어에겐 여차저차;
                        // ...

                        moveRq.ReturnToPool();
                        break;
                }
            }
        }
        public override void OnDisable()
        {
            // 필요 시 추가
        }
    }
}
