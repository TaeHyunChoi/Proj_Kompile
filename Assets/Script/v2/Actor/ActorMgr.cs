  using Kompile.Entity;
using UnityEngine;

namespace Kompile.Manager
{
    using Data;
    using Provider;

    public class ActorMgr : GameLogicMgrBase
    {
        private Transform _rootTransform;

        private AnimatorOverrideController _templateAOC;
        private AssetKey _prefabKey;

        // 여기서 List<Actor> 들고 있어야겠네;
        // Actor에게 '장소를 부여' 하고.. 풀링 여자처자를..
        
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
            
            
            // set actor : for test
#if DEV_BUILD
            var request = ActorInstantiateRequest.Create(index: 1, Vector3.zero, Quaternion.identity);
            Enqueue(request);
#endif
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
            
            switch (request.Type)
            {
                case RequestType.Actor_Instantiate:
                    InDev.Log("Actor_Instantiate");
                    update &= await InstantiateAsync(request);
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

            FieldUnitTableData data = FieldUnitTableProvider.GetData(index);
            FieldUnitAnimClipContext clip = await data.GetAnimClipsAsync();
            actor.Initialize(data, clip, _templateAOC);
            // Actor.Add(actor);

            return false;
        }
    }
}
