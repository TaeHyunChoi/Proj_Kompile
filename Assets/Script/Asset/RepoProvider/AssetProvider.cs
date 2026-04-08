namespace Script.Global.Asset.Provider
{
    using MessagePack;
    using System.Collections.Generic; // 일반 Dictionary 사용 최적화
    using System.IO;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using Script.Asset.Data;

    /// <summary>
    /// [Framework] Provider 계층
    /// 에셋과 데이터의 비동기 로드, 캐싱, 풀링을 전담하는 순수 공급자 클래스입니다.
    /// Enum 기반의 맵핑 테이블을 제거하고 Data-Driven(AssetKey) 및 Type 추론 방식을 사용합니다.
    /// </summary>
    public static partial class AssetProvider
    {
        private static Dictionary<AssetKey, InstanceEntry> 
            _gameObjectInstances;

        private static Dictionary<int, AsyncOperationHandle> 
            _nonGameObjectInstances;

        private static readonly MessagePackSerializerOptions 
            _msgPackOptions = MessagePackSerializerOptions.Standard.WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);

        // typeof(T).Name 호출 시 발생하는 string 할당(GC) 방지 캐시
        private static class TypeNameCache<T> where T : Component
        {
            public static readonly string Name = typeof(T).Name;
        }

        public static void Initialize()
        {
            _gameObjectInstances    = new Dictionary<AssetKey, InstanceEntry>();
            _nonGameObjectInstances = new Dictionary<int, AsyncOperationHandle>();
        }

        #region Game Object (Instance & Pooling)

        // Track 1. Data-Driven 방식 (콘텐츠 에셋용)
        public static async Task<GameObject> GetOrNewInstanceAsync(AssetKey addressKey, Transform parent = null, bool usePooling = true)
        {
            if (false == addressKey.IsValid)
            {
                Debug.LogError("[AssetProvider] Addressable Key is null or empty!");
                return null;
            }

            return await GetOrNewInstanceInternalAsync(addressKey, parent, usePooling);
        }
        public static void ReleaseInstance(AssetKey addressKey, GameObject instance, bool forcedDestroy = false)
        {
            ReleaseInstanceInternal(addressKey, instance, forcedDestroy);
        }


        // Track 2. Type-Inference 방식 (시스템 및 고유 UI 에셋용)
        public static async Task<T> GetOrNewInstanceAsync<T>(Transform parent = null, bool usePooling = true) where T : Component
        {
            AssetKey addressKey = new AssetKey(TypeNameCache<T>.Name);
            GameObject instance = await GetOrNewInstanceInternalAsync(addressKey, parent, usePooling);

            if (instance != null)
            {
                return instance.GetComponent<T>();
            }

            return null;
        }
        public static void ReleaseInstance<T>(GameObject instance, bool forcedDestroy = false) where T : Component
        {
            AssetKey addressKey = new AssetKey(TypeNameCache<T>.Name);
            ReleaseInstanceInternal(addressKey, instance, forcedDestroy);
        }


        // Internal Logic (공통 코어 로직)
        private static async Task<GameObject> GetOrNewInstanceInternalAsync(AssetKey key, Transform parent, bool usePooling)
        {
            if (!_gameObjectInstances.TryGetValue(key, out InstanceEntry entry))
            {
                var handle = Addressables.LoadAssetAsync<GameObject>(key.Value);
                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[AssetProvider] Failed to load: {key.Value}");
                    return null;
                }

                entry = new InstanceEntry(handle, usePooling);
                _gameObjectInstances.TryAdd(key, entry);
            }

            if (entry.TryGetPooledInstance(out GameObject instance))
            {
                instance.transform.SetParent(parent);
                instance.SetActive(true);
            }
            else
            {
                var instHandle = Addressables.InstantiateAsync(key.Value, parent);
                instance = await instHandle.Task;
            }

            entry.AddReference();
            return instance;
        }
        private static void ReleaseInstanceInternal(AssetKey key, GameObject instance, bool forcedDestroy)
        {
            if (!key.IsValid || !_gameObjectInstances.TryGetValue(key, out InstanceEntry entry))
            {
                if (instance != null) Addressables.ReleaseInstance(instance);
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

            if (entry.ShouldRelease())
            {
                Addressables.Release(entry.Handle);
                _gameObjectInstances.Remove(key);
            }
        }

        #endregion

        #region Non-GameObject Assets (Data Centric)

        public static async Task<T> LoadAssetAsync<T>(AssetKey key) where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetAsync<T>(key.Value);
            T result = await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _nonGameObjectInstances.TryAdd(result.GetInstanceID(), handle);
                return result;
            }

            return null;
        }

        public static async Task<T> LoadBinaryDataAsync<T>(AssetKey key)
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(key.Value);
            TextAsset textAsset = await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || textAsset == null)
            {
                if (handle.IsValid()) Addressables.Release(handle);
                throw new FileNotFoundException($"[AssetProvider] Binary file not found: {key.Value}");
            }

            try
            {
                return MessagePackSerializer.Deserialize<T>(textAsset.bytes, _msgPackOptions);
            }
            finally
            {
                Addressables.Release(handle);
            }
        }

        public static void ReleaseAsset(int instanceID)
        {
            if (_nonGameObjectInstances.Remove(instanceID, out var handle))
            {
                Addressables.Release(handle);
            }
        }

        public static async Awaitable<T> ReadBinaryDataAsync<T>(string key)
        {
            // ★ 복구됨: Addressables 예외(Exception)를 막기 위한 안전장치.
            // Manager의 블랙리스트 처리 덕분에 없는 키에 대해서는 단 1회만 호출되므로 최적화가 보장됩니다.
            var locHandle = Addressables.LoadResourceLocationsAsync(key);
            var locations = await locHandle.Task;

            if (locations == null || locations.Count == 0)
            {
                if (locHandle.IsValid()) Addressables.Release(locHandle);
                return default; // null 반환 시 Manager가 블랙리스트에 등록함
            }
            if (locHandle.IsValid()) Addressables.Release(locHandle);

            // 2. 실제 에셋 로드 (안전하게 호출됨)
            var handle = Addressables.LoadAssetAsync<TextAsset>(key);
            TextAsset textAsset = await handle.Task;

            try
            {
                if (handle.Status != AsyncOperationStatus.Succeeded || textAsset == null)
                {
                    return default;
                }

                // 정적으로 캐싱된 옵션 사용
                return MessagePackSerializer.Deserialize<T>(textAsset.bytes, _msgPackOptions);
            }
            finally
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
        }

        #endregion
    }
}