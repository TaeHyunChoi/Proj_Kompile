using Kompile.Unit.Entity;

namespace Kompile.Asset.Provider
{
    using MessagePack;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceLocations;
    using Kompile.Asset.Data;
    using Kompile.Asset.Utility;

    /// <summary>
    /// 에셋과 데이터의 비동기 로드, 캐싱, 풀링을 전담하는 순수 공급자 클래스.<br/>
    /// Enum 기반의 맵핑 테이블을 제거하고 Data-Driven(AssetKey) 및 Type 추론 방식을 사용
    /// </summary>
    public static partial class AssetProvider
    {
        private static readonly Dictionary<AssetKey, InstanceEntryContext> 
            GameObjectInstances = new Dictionary<AssetKey, InstanceEntryContext>();

        private static readonly Dictionary<int, AsyncOperationHandle> 
            NonGameObjectInstances = new Dictionary<int, AsyncOperationHandle>();

// 💡 커스텀 포매터가 포함된 CompositeResolver로 교체
        private static readonly MessagePackSerializerOptions MsgPackOptions = 
            MessagePackSerializerOptions.Standard.WithResolver(
                MessagePack.Resolvers.CompositeResolver.Create(
                    new MessagePack.Formatters.IMessagePackFormatter[] { new FixedString32BytesFormatter() },
                    new MessagePack.IFormatterResolver[] { MessagePack.Resolvers.ContractlessStandardResolver.Instance }
                )
            );
        // typeof(T).Name 호출 시 발생하는 string 할당(GC) 방지 캐시
        private static class TypeNameCache<T> where T : Component
        {
            public static readonly string Name = typeof(T).Name;
        }

        #region Game Object (Instance & Pooling)

        // Track 1. Data-Driven 방식 (콘텐츠 에셋용)
        public static async Awaitable<GameObject> GetOrNewInstanceAsync(AssetKey addressKey, Transform parent = null, bool usePooling = true)
        {
            if (addressKey.IsValid)
            {
                return await GetOrNewInstanceInternalAsync(addressKey, parent, usePooling);
            }

            Debug.LogError("[AssetProvider] Addressable Key is null or empty!");
            return null;

        }

        /// <summary>
        /// 유닛 프리팹을 풀에서 꺼내거나 새로 인스턴스화하여 컴포넌트를 추가한 뒤 반환합니다.
        /// Initialize는 호출하지 않습니다. 호출자가 직접 Initialize를 호출해야 합니다.
        /// </summary>
        public static async Awaitable<TEntity> GetOrNewUnitInstanceAsync<TEntity>(Transform root)
            where TEntity : UnitEntityBase
        {
            AssetKey prefabKey = new AssetKey(AssetConst.UNIT_PREFAB);
            GameObject go = await AssetProvider.GetOrNewInstanceAsync(prefabKey, root);
            if (!go)
            {
                Debug.LogError("[AssetProvider] 유닛 프리팹 로드 실패");
                return null;
            }

            TEntity entity = go.AddComponent<TEntity>();
            entity.SetAssetKey(prefabKey);
            return entity;
        }

        public static void ReleaseInstance(AssetKey addressKey, GameObject instance, bool forcedDestroy = false)
        {
            ReleaseInstanceInternal(addressKey, instance, forcedDestroy);
        }


        // Track 2. Type-Inference 방식 (시스템 및 고유 UI 에셋용)
        public static async Awaitable<T> GetOrNewInstanceAsync<T>(Transform parent = null, bool usePooling = true) where T : Component
        {
            AssetKey addressKey = new AssetKey(TypeNameCache<T>.Name);
            GameObject instance = await GetOrNewInstanceInternalAsync(addressKey, parent, usePooling);

            return instance ? instance.GetComponent<T>() : null;
        }
        public static void ReleaseInstance<T>(GameObject instance, bool forcedDestroy = false) where T : Component
        {
            AssetKey addressKey = new AssetKey(TypeNameCache<T>.Name);
            ReleaseInstanceInternal(addressKey, instance, forcedDestroy);
        }


        // Internal Logic (공통 코어 로직)
        private static async Awaitable<GameObject> GetOrNewInstanceInternalAsync(AssetKey key, Transform parent, bool usePooling)
        {
            if (!GameObjectInstances.TryGetValue(key, out InstanceEntryContext entry))
            {
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key.Value);
                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[AssetProvider] Failed to load: {key.Value}");
                    return null;
                }

                entry = new InstanceEntryContext(handle, usePooling);
                GameObjectInstances.TryAdd(key, entry);
            }

            if (entry.TryGetPooledInstance(out GameObject instance))
            {
                instance.transform.SetParent(parent);
                instance.SetActive(true);
            }
            else
            {
                AsyncOperationHandle<GameObject> instHandle = Addressables.InstantiateAsync(key.Value, parent);
                instance = await instHandle.Task;
            }

            entry.AddReference();
            return instance;
        }
        private static void ReleaseInstanceInternal(AssetKey key, GameObject instance, bool forcedDestroy)
        {
            if (!key.IsValid || !GameObjectInstances.TryGetValue(key, out InstanceEntryContext entry))
            {
                if (instance)
                {
                    Addressables.ReleaseInstance(instance);
                }
                
                return;
            }

            if (entry.UsePooling && !forcedDestroy)
            {
                instance.SetActive(false);
                instance.transform.SetParent(null);
                entry.ReturnToPool(instance);
            }
            else
            {
                Addressables.ReleaseInstance(instance);
            }

            entry.RemoveReference();

            if (!entry.ShouldRelease())
            {
                return;
            }

            Addressables.Release(entry.Handle);
            GameObjectInstances.Remove(key);
        }

        #endregion

        #region Non-GameObject Assets (Data Centric)

        public static async Awaitable<T> LoadAssetAsync<T>(AssetKey key) where T : UnityEngine.Object
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key.Value);
            T result = await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                return null;
            }

            NonGameObjectInstances.TryAdd(result.GetInstanceID(), handle);
            return result;

        }

        public static void ReleaseAsset(int instanceID)
        {
            if (NonGameObjectInstances.Remove(instanceID, out var handle))
            {
                Addressables.Release(handle);
            }
        }

        public static async Awaitable<T> ReadBinaryDataAsync<T>(string key)
        {
            // ★ 복구됨: Addressables 예외(Exception)를 막기 위한 안전장치.
            // Manager의 블랙리스트 처리 덕분에 없는 키에 대해서는 단 1회만 호출되므로 최적화가 보장됩니다.
            AsyncOperationHandle<IList<IResourceLocation>> locHandle = Addressables.LoadResourceLocationsAsync(key);
            IList<IResourceLocation> locations = await locHandle.Task;

            if (locations == null 
                || locations.Count == 0)
            {
                if (locHandle.IsValid())
                {
                    Addressables.Release(locHandle);                    
                }
                
                return default; // null 반환 시 Manager가 블랙리스트에 등록함
            }

            if (locHandle.IsValid())
            {
                Addressables.Release(locHandle);                
            }
            
            // 2. 실제 에셋 로드 (안전하게 호출됨)
            AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(key);
            TextAsset textAsset = await handle.Task;

            try
            {
                if (handle.Status != AsyncOperationStatus.Succeeded || !textAsset)
                {
                    return default;
                }

                // 정적으로 캐싱된 옵션 사용
                return MessagePackSerializer.Deserialize<T>(textAsset.bytes, MsgPackOptions);
            }
            finally
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);                    
                }
            }
        }

        #endregion
    }
}