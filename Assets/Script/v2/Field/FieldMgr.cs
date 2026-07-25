using Kompile.Data;
using Kompile.Provider;
using System.Collections.Generic;
using UnityEngine;

namespace Kompile.Manager
{
    public class FieldMgr : GameLogicMgrBase
    {
        private HashSet<int> _validGridKeys;

        public FieldMgr() 
        {
            // 이게 되네;
            _ = InitializeAsync();

            //Awaitable<MapRegistryData> loadRegistry = AssetProvider.ReadBinaryDataAsync<MapRegistryData>("MapRegistry");
            //var awaiter = loadRegistry.GetAwaiter();
            //awaiter.OnCompleted(() => 
            //{
            //    var registryData = awaiter.GetResult();
            //    if (null == registryData
            //        || null == registryData.BakedGridKeys)
            //    {
            //        return;
            //    }

            //    var arr = registryData.BakedGridKeys;
            //    int count = registryData.BakedGridKeys.Length;
            //    _validGridKeys = new HashSet<int>(count);
            //    for (int i = 0; i < count; ++i)
            //    {
            //        _validGridKeys.Add(arr[i]);
            //    }


            //    InGame.AddMgr(this);
            //});
        }

        private async Awaitable InitializeAsync()
        {
            MapRegistryData registryData = await AssetProvider.ReadBinaryDataAsync<MapRegistryData>("MapRegistry");
            if (null == registryData)
            {
                return;
            }

            var bakedKeys = registryData.BakedGridKeys;
            if (null == bakedKeys || 0 >= bakedKeys.Length)
            {
                return;
            }

            int count = bakedKeys.Length;
            _validGridKeys = new HashSet<int>(count);
            for (int i = 0; i < count; ++i)
            {
                _validGridKeys.Add(bakedKeys[i]);
            }

            // 사실 저건 map의 영역...
            // 이라고 하긴엔 카메라 조작
            // 또 한 번 개념 정립이 필요하겠구나...?


            InGame.AddMgr(this);
        }

        public override void OnUpdate()
        {
            // 얘는 
        }
    }
}
