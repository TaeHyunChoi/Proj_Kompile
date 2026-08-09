namespace Kompile.Manager
{
    using Data;
    using Provider;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class FieldMgr : GameLogicMgrBase
    {
        private HashSet<int> _mapRegistry;
        private MapMgr _mapMgr;

        // --- override ---
        public override void RegisterToCache()
        {
            InGame.Register(this);
        }
        public override async Awaitable<bool> OnAwake()
        {
#if DEV_BUILD
            InDev.Log($"Call FieldMgr.OnAwake()");
#endif
            
            Prior = 2;

            bool result = true;
            try
            {
                result &= await InitAsync_MapRegistry();

                _mapMgr = new MapMgr();
                result &= await _mapMgr.OnAwake();
            }
            catch (Exception e)
            {
                InDev.LogError(e.Message);
                return false;
            }

            if (result)
            {
                _mapMgr.PlayStreaming(_mapRegistry);
            }
            return result;
        }
        public override async Awaitable<bool> OnUpdate()
        {
            bool update = true;
            update &= await ProcessRequests();

            // 맵 갱신
            _mapMgr.OnUpdate();

            // 필드의 액터 업데이트 요청
            ActorUpdateRequest request = RequestProvider<ActorUpdateRequest>.Get();
            InGame.Actor.Enqueue(request);

            // etc ...

            return update;
        }
        public override void OnDisable()
        {
            _mapMgr.OnDisable();
        }

        protected override async Awaitable<bool> HandleRequestAsync(RequestBase request)
        {
            await Awaitable.NextFrameAsync();
            return false;
        }


        // --- function ---
        private async Awaitable<bool> InitAsync_MapRegistry()
        {
            MapRegistryData registryData = await AssetProvider.ReadBinaryDataAsync<MapRegistryData>("MapRegistry");
            if (null == registryData)
            {
                return false;
            }

            int[] bakedKeys = registryData.BakedGridKeys;
            if (null == bakedKeys || 0 >= bakedKeys.Length)
            {
                return false;
            }

            int count = bakedKeys.Length;
            _mapRegistry = new HashSet<int>(count);
            for (int i = 0; i < count; ++i)
            {
                _mapRegistry.Add(bakedKeys[i]);
            }

            return true;
        }
    }
}
