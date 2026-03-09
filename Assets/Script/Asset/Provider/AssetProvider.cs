namespace Script.Asset.Provider
{
    using MessagePack;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using Script.Asset.Data;
    
    public static partial class AssetProvider
    {
        // Enum 타입별 AssetMap 캐시 (Provider의 핵심: 데이터 공급원)
        private static Dictionary<Type, ScriptableObject> _assetMapCache;

        // 생성된 게임 오브젝트(InstanceEntry) 관리 (Key: Address)
        private static ConcurrentDictionary<string, InstanceEntry> _gameObjectInstances;

        // 일반 에셋(ScriptableObject, Texture 등) 핸들 관리 (Key: InstanceID)
        private static ConcurrentDictionary<int, AsyncOperationHandle> _nonGameObjectInstances;

        /// <summary>
        /// 시스템 초기화. 초기 구동 시 반드시 호출해야 합니다.
        /// </summary>
        public static void Initialize()
        {
            _assetMapCache = new Dictionary<Type, ScriptableObject>();
            LoadAllAssetMaps();

            _gameObjectInstances = new ConcurrentDictionary<string, InstanceEntry>();
            _nonGameObjectInstances = new ConcurrentDictionary<int, AsyncOperationHandle>();
            
            Debug.Log("[AssetProvider] Initialized.");
        }

        #region AssetMap Logic (Value-Centric Data Supply)

        private static void LoadAllAssetMaps()
        {
            // Resources/AssetMap 내의 ScriptableObject 로드
            ScriptableObject[] maps = Resources.LoadAll<ScriptableObject>("AssetMap");

            foreach (var map in maps)
            {
                Type mapType = map.GetType();
                Type baseType = mapType.BaseType;

                // AssetMapBase<T> 상속 확인 및 Generic Argument 추출
                while (baseType != null && baseType.IsGenericType && baseType.GetGenericTypeDefinition() != typeof(AssetMapBase<>))
                {
                    baseType = baseType.BaseType;
                }

                if (baseType != null && baseType.IsGenericType)
                {
                    Type enumType = baseType.GetGenericArguments()[0];
                    if (!_assetMapCache.ContainsKey(enumType))
                    {
                        _assetMapCache.Add(enumType, map);

                        // 인터페이스를 통한 초기화 (IInitializable은 Utilities 또는 Datas에 정의 권장)
                        if (map is IInitializable initializable)
                        {
                            initializable.Initialize();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enum ID에 해당하는 어드레서블 주소를 반환합니다.
        /// </summary>
        public static string GetAssetAddress<TEnum>(TEnum id) where TEnum : Enum
        {
            if (_assetMapCache.TryGetValue(typeof(TEnum), out ScriptableObject map))
            {
                if (map is AssetMapBase<TEnum> assetMap)
                {
                    return assetMap.GetAddressKey(id);
                }
            }
            Debug.LogError($"[AssetProvider] AssetMap not found for type: {typeof(TEnum).Name}");
            return null;
        }

        #endregion

        #region Game Object (Instance & Pooling)

        /// <summary>
        /// ID를 통해 인스턴스를 비동기로 획득합니다.
        /// </summary>
        public static async Task<GameObject> GetOrNewInstanceAsync<TEnum>(TEnum id, Transform parent = null, bool usePooling = true) where TEnum : Enum
        {
            string address = GetAssetAddress(id);
            if (string.IsNullOrEmpty(address)) return null;

            return await GetOrNewInstanceInternalAsync(address, parent, usePooling);
        }

        private static async Task<GameObject> GetOrNewInstanceInternalAsync(string key, Transform parent, bool usePooling)
        {
            if (!_gameObjectInstances.TryGetValue(key, out InstanceEntry entry))
            {
                var handle = Addressables.LoadAssetAsync<GameObject>(key);
                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[AssetProvider] Failed to load: {key}");
                    return null;
                }

                entry = new InstanceEntry(handle, usePooling);
                _gameObjectInstances.TryAdd(key, entry);
            }

            GameObject instance;
            if (entry.HasPooledInstance())
            {
                instance = entry.Pool.Dequeue();
                instance.transform.SetParent(parent);
                instance.SetActive(true);
            }
            else
            {
                var instHandle = Addressables.InstantiateAsync(key, parent);
                instance = await instHandle.Task;
            }

            entry.AddReference();
            return instance;
        }

        /// <summary>
        /// 인스턴스를 반환하여 풀에 넣거나 해제합니다.
        /// </summary>
        public static void ReleaseInstance<TEnum>(TEnum id, GameObject instance, bool forcedDestroy = false) where TEnum : Enum
        {
            string address = GetAssetAddress(id);
            ReleaseInstanceInternal(address, instance, forcedDestroy);
        }

        private static void ReleaseInstanceInternal(string key, GameObject instance, bool forcedDestroy)
        {
            if (string.IsNullOrEmpty(key) || !_gameObjectInstances.TryGetValue(key, out InstanceEntry entry))
            {
                if (instance != null) Addressables.ReleaseInstance(instance);
                return;
            }

            if (entry.UsePooling && !forcedDestroy)
            {
                instance.SetActive(false);
                instance.transform.SetParent(null);
                entry.Pool.Enqueue(instance);
            }
            else
            {
                Addressables.ReleaseInstance(instance);
            }

            entry.RemoveReference();

            if (entry.ShouldRelease())
            {
                Addressables.Release(entry.Handle);
                _gameObjectInstances.TryRemove(key, out _);
            }
        }

        #endregion

        #region Non-GameObject Assets (Data Centric)

        public static async Task<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            T result = await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _nonGameObjectInstances.TryAdd(result.GetInstanceID(), handle);
                return result;
            }
            return null;
        }

        /// <summary>
        /// 바이너리 데이터를 로드하여 역직렬화합니다. (Runtime Data 가공)
        /// </summary>
        public static async Task<T> LoadBinaryDataAsync<T>(string key)
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(key);
            TextAsset textAsset = await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || textAsset == null)
            {
                if (handle.IsValid()) Addressables.Release(handle);
                throw new FileNotFoundException($"[AssetProvider] Binary file not found: {key}");
            }

            try
            {
                byte[] bytes = textAsset.bytes;
                var options = MessagePackSerializerOptions.Standard.WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                return MessagePackSerializer.Deserialize<T>(bytes, options);
            }
            finally
            {
                Addressables.Release(handle);
            }
        }

        public static void ReleaseAsset(int instanceID)
        {
            if (_nonGameObjectInstances.TryRemove(instanceID, out var handle))
            {
                Addressables.Release(handle);
            }
        }

        #endregion

        public static async Awaitable LoadAllDatatable()
        {

        }
    }
}