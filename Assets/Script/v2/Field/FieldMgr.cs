using Kompile.Data;
using Kompile.Provider;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kompile.Manager
{
    public class FieldMgr : GameLogicMgrBase
    {
        private HashSet<int> _mapRegistry;

        // --- override ---
        public override async Awaitable<bool> OnAwake()
        {
            Prior = 2;

            bool result = true;
            try
            {
                result &= await InitAsync_MapRegistry();
            }
            catch (Exception e)
            {
                InDev.LogError(e.Message);
                return false;
            }
            
            return result;
        }
        public override void OnUpdate()
        {
            
        }

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
