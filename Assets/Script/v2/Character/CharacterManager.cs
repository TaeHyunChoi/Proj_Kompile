using System.Collections.Generic;
using UnityEngine;
using Kompile.Data;
using Kompile.Provider;

namespace Kompile.Manager
{
    public class CharacterManager : GameLogicManagerBase
    {
        // OnUpdate: 매 프레임마다 '게임 논리를 처리'한다. 
        // 이후 OnFixedUpdate, OnLateUpdate() 등으로 연산을 넘긴다...
        // 유닛 ai도 여기서 처리를 해야 하는구나;

        // 구조를 다시 잡아본다면... system.input -> [Logic] OnUpdate -> [physical] OnFixedUpdate -> [graphic] OnLateUpdate -> ... 인건데
        // 누가, 어디서, 어떻게 Request를 날리는지도 생각해봐야 하는구나.

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
    }
}
