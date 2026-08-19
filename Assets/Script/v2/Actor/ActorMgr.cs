namespace Kompile.Manager
{
    using UnityEngine;
    using Entity;
    using Data;
    using Provider;
    using System.Collections.Generic;

    public class ActorMgr : GameLogicMgrBase
    {
        private Transform _rootTransform;

        private AnimatorOverrideController _templateAOC;
        private AssetKey _prefabKey;

        private List<ActorEntity> _actors = new List<ActorEntity>();
        private ActorEntity _playerActor;


        // --- override ---
        public override void RegisterToCache()
        {
            InGame.Register<ActorMgr>(this);
        }
#pragma warning disable 1998
        public override async Awaitable<bool> OnAwake()
        {
            Prior = 1;

            // root
            _rootTransform = new GameObject("Unit").transform;
            _rootTransform.SetParent(InGame.Transform);
            _rootTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _templateAOC = await AssetProvider.LoadAssetAsync<AnimatorOverrideController>(new AssetKey("aoc_field_unit"));

            // cache asset
            _prefabKey = new AssetKey(AssetConst.ACTOR_PREFAB);

            return true;
        }
#pragma warning restore 1998
        public override async Awaitable<bool> OnUpdate()
        {
            return await ProcessRequests();
        }

        public override void OnDisable()
        {
            // 필요 시 추가
        }

        protected override async Awaitable<bool> HandleRequestAsync(RequestBase request)
        {
            bool update = true;
            float deltaTime = Time.deltaTime;

            switch (request.Type)
            {
                case RequestType.Actor_Instantiate:
                    update &= await InstantiateAsync(request);
                    break;
                case RequestType.Actor_PlayerUpdate:
                    Definition.InputState input = InGame.Input.Current;
                    if (input.IsPressing(Definition.IDxInput.MOVE_ALL))
                    {
                        Debug.Log($"[DEBUG] {input.Current}");
                    }
                    break;
                case RequestType.Actor_Update:
                    for (int i = 0; i < _actors.Count; ++i)
                    {
                        update &= _actors[i].OnUpdate(deltaTime);
                    }
                    break;

                default:
                    break;
            }
            
            request.ReturnToPool();
            return update;
        }
        
        
        // --- function ---
        private async Awaitable<bool> InstantiateAsync(RequestBase request)
        {
            // get request info
            var req = request as ActorInstantiateRequest;
            int index = req.Index;
            Vector3 position = req.Position;
            Quaternion quaternion = req.Quaternion;

            // instantiate
            ActorEntity actor = await AssetProvider.GetOrNewEntityInstanceAsync<ActorEntity>(_prefabKey, _rootTransform);
            if (!actor)
            {
                return false;
            }

            actor.transform.SetPositionAndRotation(position, quaternion);

            FieldUnitTableData data = ActorDataProvider.GetData(index);
            FieldUnitAnimClipContext clip = await data.GetAnimClipsAsync();
            actor.Initialize(data, clip, _templateAOC, req.MapProvider);
            _actors.Add(actor);

            if (UnitBrainType.Player == actor.BrainType)
            {
                _playerActor = actor;
            }

            return true;
        }
    }
}
