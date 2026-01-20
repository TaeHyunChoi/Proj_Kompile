namespace Script.Asset
{
    using MessagePack;
    using MessagePack.Resolvers;
    using Script.Data;
    using Script.Map;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    /// <summary>
    /// [Framework] Addressable Asset을 관리하고 제공하는 시스템입니다.
    /// AssetMap을 통해 Enum 키로 에셋을 로드하며, 게임 오브젝트 풀링 및 바이너리 로드를 지원합니다.
    /// 그러니까, 얘는 (1)메모리 참조 (2)인스턴스 생성 역할만 한다. 그 외엔 아무 역할을 맡지 않는다.
    /// </summary>
    public static partial class AssetSystem
    {
        // Enum 타입별 AssetMap 캐시 (예: PrefabID -> AssetMapBase<PrefabID>)
        private static Dictionary<Type, ScriptableObject> _assetMapCache;

        // 생성된 게임 오브젝트(InstanceEntry) 관리 (Key: Address)
        private static ConcurrentDictionary<string, InstanceEntry> _gameObjectInstances;

        // 일반 에셋(ScriptableObject, Texture 등) 핸들 관리 (Key: InstanceID)
        private static ConcurrentDictionary<int, AsyncOperationHandle> _nonGameObjectInstances;

        /// <summary>
        /// 시스템 초기화. 게임 시작 시(OpeningManager 등)에서 반드시 호출해야 합니다.
        /// </summary>
        public static void Initialize()
        {
            _assetMapCache = new Dictionary<Type, ScriptableObject>();
            LoadAllAssetMaps(); // AssetMapBase 로드 및 초기화

            _gameObjectInstances = new ConcurrentDictionary<string, InstanceEntry>();
            _nonGameObjectInstances = new ConcurrentDictionary<int, AsyncOperationHandle>();
        }

        #region AssetMap Logic

        private static void LoadAllAssetMaps()
        {
            // Resources/AssetMap 폴더 내의 모든 ScriptableObject 로드
            ScriptableObject[] maps = Resources.LoadAll<ScriptableObject>("AssetMap");

            foreach (var map in maps)
            {
                Type mapType = map.GetType();
                Type baseType = mapType.BaseType;

                // AssetMapBase<T>를 상속받았는지 확인하여 Generic Argument(Enum Type)를 추출
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

                        // 런타임 딕셔너리(RuntimeMap) 생성
                        (map as IInitializable)?.Initialize();
                    }
                }
            }
        }

        /// <summary>
        /// Enum ID에 해당하는 어드레서블 주소(Key)를 반환합니다.
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
            Debug.LogError($"[AssetSystem] AssetMap not found or invalid for type: {typeof(TEnum).Name}");
            return null;
        }

        #endregion

        #region Game Object (Instance & Pooling)

        /// <summary>
        /// Enum ID를 통해 게임 오브젝트 인스턴스를 가져옵니다. (풀링 지원)
        /// 사용 예: await AssetSystem.GetOrNewInstanceAsync(PrefabID.UNIT_BASE, parent);
        /// </summary>
        public static async Task<GameObject> GetOrNewInstanceAsync<TEnum>(TEnum id, Transform parent = null, bool usePooling = true) where TEnum : Enum
        {
            string address = GetAssetAddress(id);

            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"[AssetSystem] Address is null for ID: {id}");
                return null;
            }

            return await GetOrNewInstanceInternalAsync(address, parent, usePooling);
        }
        private static async Task<GameObject> GetOrNewInstanceInternalAsync(string key, Transform parent, bool usePooling)
        {
            // 1. InstanceEntry 확인 및 없으면 최초 로드
            if (false == _gameObjectInstances.TryGetValue(key, out InstanceEntry entry))
            {
                var handle = Addressables.LoadAssetAsync<GameObject>(key);
                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[AssetSystem] Failed to load asset: {key}");
                    return null;
                }

                // * 중요: 여기서 handle.Result(프리팹)를 _nonGameObjectInstances에 넣지 않습니다.
                // 핸들 관리는 오직 InstanceEntry가 담당합니다.

                entry = new InstanceEntry(handle, usePooling);
                _gameObjectInstances.TryAdd(key, entry);
            }

            // 2. 인스턴스 생성 또는 풀에서 가져오기
            GameObject instance;
            if (entry.HasPooledInstance())
            {
                instance = entry.Pool.Dequeue();
                instance.transform.SetParent(parent);
                instance.SetActive(true);
            }
            else
            {
                // Addressables의 참조 카운팅을 위해 InstantiateAsync 사용
                var instHandle = Addressables.InstantiateAsync(key, parent);
                instance = await instHandle.Task;
            }

            // 3. 참조 카운트 증가
            entry.AddReference();
            return instance;
        }

        /// <summary>
        /// 문자열 주소로 직접 로드 (필요한 경우 사용)
        /// </summary>
        public static async Task<GameObject> GetOrNewInstanceAsync(string address, Transform parent = null, bool usePooling = true)
        {
            return await GetOrNewInstanceInternalAsync(address, parent, usePooling);
        }

        /// <summary>
        /// 인스턴스를 반환합니다. 풀링 대상이면 비활성화 후 풀로 돌아가고, 아니면 파괴됩니다.
        /// </summary>
        public static void ReleaseInstance<TEnum>(TEnum id, GameObject instance, bool forcedDestroy = false) where TEnum : Enum
        {
            string address = GetAssetAddress(id);
            ReleaseInstanceInternal(address, instance, forcedDestroy);
        }

        /// <summary>
        /// IngameMonoBehaviourBase를 상속받은 컴포넌트 해제 편의 함수.
        /// (instance.PrefabID 프로퍼티가 존재해야 합니다)
        /// </summary>
        public static void ReleaseInstance(IngameMonoBehaviourBase instance, bool forcedDestroy = false)
        {
            if (instance == null)
            {
                return;
            }

            ReleaseInstance(instance.PrefabID, instance.gameObject, forcedDestroy);
        }

        private static void ReleaseInstanceInternal(string key, GameObject instance, bool forcedDestroy)
        {
            if (string.IsNullOrEmpty(key) || !_gameObjectInstances.TryGetValue(key, out InstanceEntry entry))
            {
                // 관리되지 않는 객체라면 즉시 해제
                if (instance != null) Addressables.ReleaseInstance(instance);
                return;
            }

            // 풀링 처리
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

            // 참조가 0이고 풀링된 객체도 없다면 핸들 자체를 해제하여 메모리 확보
            if (entry.ShouldRelease())
            {
                Addressables.Release(entry.Handle);
                _gameObjectInstances.TryRemove(key, out _);
                Debug.Log($"[AssetSystem] Released Asset Handler: {key}");
            }
        }

        #endregion

        #region Map Object
        public static async void LoadMapData()
        {
            string label = "MapNavi";
            var handle = Addressables.LoadAssetsAsync<TextAsset>(label, callback: (textAsset) =>
            {
                // 칵 파일이 로드될 때마다 실행되는 콜백 (병렬 실행)
                if (null != textAsset)
                {
                    var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
                    MapGridData grid = MessagePackSerializer.Deserialize<MapGridData>(textAsset.bytes, options);

                    // just for debugging
                    //Debug.Log($"[Load Baked Map ] {textAsset.name}");
                    //int gKey = grid.Key;
                    //foreach (var tKV in grid.NaviTileDict)
                    //{
                    //    int tKey = tKV.Key;
                    //    var tile = tKV.Value;
                    //    long id = MapPathUtil.ComputeID(gKey, tKey);
                    //    Debug.Log($"{MapPathUtil.ComputeWorldPosition(id)} nav:{System.Convert.ToString(tile.NaviMask, 16)}, link:{System.Convert.ToString(tile.LinkMask, 2)}");
                    //}
                }
            });
            await handle.Task;
        }
        #endregion

        #region Non-GameObject Assets (ScriptableObject, Texture, Data)

        public static async Task<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            T result = await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // 일반 에셋은 InstanceID를 키로 관리하여 ReleaseAsset(obj.GetInstanceID())로 해제 가능하게 함
                _nonGameObjectInstances.TryAdd(result.GetInstanceID(), handle);
                return result;
            }

            Debug.LogError($"[AssetSystem] Failed load asset: {key}");
            return null;
        }

        public static async Task<T> LoadBinaryDataAsync<T>(string key)
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(key);
            TextAsset textAsset = await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || textAsset == null)
            {
                if (handle.IsValid()) Addressables.Release(handle);
                throw new FileNotFoundException($"[AssetSystem] Binary file not found: {key}");
            }

            try
            {
                byte[] bytes = textAsset.bytes;
                // 직렬화 옵션 (ContractlessStandardResolver 사용)
                var options = MessagePackSerializerOptions.Standard.WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                return MessagePackSerializer.Deserialize<T>(bytes, options);
            }
            finally
            {
                // 데이터 추출이 끝났으므로 TextAsset 메모리는 즉시 해제
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

        #region Internal Class (Instance Entry)

        private class InstanceEntry
        {
            public AsyncOperationHandle<GameObject> Handle { get; private set; }
            public Queue<GameObject> Pool { get; private set; }
            public bool UsePooling { get; private set; }
            public int ReferenceCount { get; private set; }

            public InstanceEntry(AsyncOperationHandle<GameObject> handle, bool usePooling)
            {
                Handle = handle;
                UsePooling = usePooling;
                Pool = new Queue<GameObject>();
                ReferenceCount = 0;
            }

            public bool HasPooledInstance()
            {
                while (Pool.Count > 0)
                {
                    if (Pool.Peek() != null) return true;
                    Pool.Dequeue(); // 파괴된 객체가 있다면 제거
                }
                return false;
            }

            public void AddReference() => ReferenceCount++;
            public void RemoveReference() => ReferenceCount = Mathf.Max(0, ReferenceCount - 1);

            public bool ShouldRelease()
            {
                // 참조 카운트가 0이고 풀에 남은 객체도 없을 때 핸들을 완전히 해제
                return ReferenceCount <= 0 && Pool.Count == 0;
            }
        }

        #endregion

        public static async Awaitable LoadAllDatatable()
        {

        }
    }
}