using UnityEngine;

namespace Kompile.Manager
{
    using Data;

    public class ActorMgr : GameLogicMgrBase
    {
        private Transform _rootTransform;

        // --- override ---
        public override void RegisterToCache()
        {
            InGame.Register<ActorMgr>(this);
        }
#pragma warning disable 1998
        public override async Awaitable<bool> OnAwake()
        {
            Prior = 1;

            _rootTransform = new GameObject("Unit").transform;
            _rootTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var request = ActorInstantiateRequest.Get(index: 1);
            InGame.Actor.Enqueue(request);

            return true;
        }
#pragma warning restore 1998
        public override void OnUpdate()
        {
            //GameLogicMgrBase를 상속;
            ProcessRequests();
        }
        public override void OnDisable()
        {
            // 필요 시 추가
        }

        public void UpdateInField()
        {
            // 필드에 있는 액터들의 상태를 갱신
            // (이동, 애니메이션, 스킬 등)


        }

        // 여기도 다시 고민.
        // 대용량 이동 제어도 사실 필요 없는데; (해봤자 100개 정도일 듯?)
        //private async Awaitable<x_FieldEntity> SpawnFieldEntityAsync(int index)
        //{
        //    x_FieldEntity fieldEntity = await AssetProvider.Field.GetOrNewEntityInstanceAsync(1, _unitRoot, _templateAOC, null);
        //    _unitMoveManager.Register(fieldEntity.MoveComponent);

        //    return fieldEntity;
        //}
    }
}
