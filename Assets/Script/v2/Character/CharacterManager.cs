using System.Collections.Generic;
using UnityEngine;
using Kompile.Data;
using Kompile.Provider;

namespace Kompile.Manager
{
    public class CharacterManager
    {
        private List<RequestBase> _inbox      = new List<RequestBase>(64);
        private List<RequestBase> _processing = new List<RequestBase>(64);

        public void Enqueue(RequestBase request)
        {
            _inbox.Add(request);
        }

        // OnUpdate: 매 프레임마다 '게임 논리를 처리'한다. 
        // 이후 OnFixedUpdate, OnLateUpdate() 등으로 연산을 넘긴다...
        // 유닛 ai도 여기서 처리를 해야 하는구나;

        // 구조를 다시 잡아본다면... system.input -> [Logic] OnUpdate -> [physical] OnFixedUpdate -> [graphic] OnLateUpdate -> ... 인건데
        // 누가, 어디서, 어떻게 Request를 날리는지도 생각해봐야 하는구나.

        public void OnUpdate()
        {
            // 들고 있는 캐릭터들의 Behaviour Tree를 결정해서 Request를 _inbox로 날리고
            // 이후 게임 논리를 수행
            // AssetProvider에서 유닛 풀링도 챙겨와야겠네...

            // 그리고? CharacterManager.OnUpdate()에서 필터링한 유닛 외에는 'pause' 기능이 필요할깝쇼?
            // (좀 더 생각해봅시다...)
            // 

            if (_inbox.Count == 0)
            {
                return;
            }

            List<RequestBase> temp = _inbox;
            _inbox = _processing;
            _processing = temp;

            for (int i = 0; i < temp.Count; ++i)
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
