namespace Script.Asset.Provider
{
    using MessagePack;
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    /// <summary>
    /// [Framework] Provider 계층
    /// 에셋과 데이터의 비동기 로드, 캐싱, 풀링을 전담하는 순수 공급자 클래스입니다.
    /// Enum 기반의 맵핑 테이블을 제거하고 Data-Driven 및 Type 추론 방식을 사용합니다.
    /// </summary>
    public static partial class AssetProvider
    {
        // 생성된 게임 오브젝트(InstanceEntry) 관리 (Key: Addressable 주소 문자열)
        private static ConcurrentDictionary<string, InstanceEntry> _gameObjectInstances;

        // 일반 에셋(ScriptableObject, Texture 등) 핸들 관리 (Key: InstanceID)
        private static ConcurrentDictionary<int, AsyncOperationHandle> _nonGameObjectInstances;

        public static void Initialize()
        {
            _gameObjectInstances = new ConcurrentDictionary<string, InstanceEntry>();
            _nonGameObjectInstances = new ConcurrentDictionary<int, AsyncOperationHandle>();

            Debug.Log("[AssetProvider] Initialized. (Data-Driven & Type-Inference Mode)");
        }

        #region Game Object (Instance & Pooling)

        // =========================================================================
        // Track 1. Data-Driven 방식 (콘텐츠 에셋용)
        // =========================================================================

        /// <summary>
        /// Addressable 키(문자열)를 통해 인스턴스를 비동기로 획득합니다.
        /// 데이터 테이블(Excel/CSV)에서 읽어온 문자열을 그대로 전달할 때 사용합니다.
        /// </summary>
        public static async Task<GameObject> GetOrNewInstanceAsync(string addressKey, Transform parent = null, bool usePooling = true)
        {
            if (string.IsNullOrEmpty(addressKey))
            {
                Debug.LogError("[AssetProvider] Addressable Key is null or empty!");
                return null;
            }

            return await GetOrNewInstanceInternalAsync(addressKey, parent, usePooling);
        }

        /// <summary>
        /// Addressable 키(문자열)를 통해 인스턴스를 반환하여 풀에 넣거나 해제합니다.
        /// </summary>
        public static void ReleaseInstance(string addressKey, GameObject instance, bool forcedDestroy = false)
        {
            ReleaseInstanceInternal(addressKey, instance, forcedDestroy);
        }


        // =========================================================================
        // Track 2. Type-Inference 방식 (시스템 및 고유 UI 에셋용)
        // =========================================================================

        /// <summary>
        /// 클래스 타입(T)을 기반으로 Addressable 주소를 추론하여 비동기로 획득합니다.
        /// 프리팹의 Addressable 주소가 해당 클래스명(typeof(T).Name)과 일치해야 합니다.
        /// </summary>
        public static async Task<T> GetOrNewInstanceAsync<T>(Transform parent = null, bool usePooling = true) where T : Component
        {
            string addressKey = typeof(T).Name;
            GameObject instance = await GetOrNewInstanceInternalAsync(addressKey, parent, usePooling);

            if (instance != null)
            {
                return instance.GetComponent<T>();
            }
            return null;
        }

        /// <summary>
        /// 클래스 타입(T)을 기반으로 인스턴스를 반환하여 풀에 넣거나 해제합니다.
        /// </summary>
        public static void ReleaseInstance<T>(GameObject instance, bool forcedDestroy = false) where T : Component
        {
            string addressKey = typeof(T).Name;
            ReleaseInstanceInternal(addressKey, instance, forcedDestroy);
        }


        // =========================================================================
        // Internal Logic (공통 코어 로직)
        // =========================================================================

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

            if (entry.TryGetPooledInstance(out GameObject instance))
            {
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
                _gameObjectInstances.TryRemove(key, out _);
            }
        }
        #endregion

        #region Non-GameObject Assets (Data Centric)
        // 기존과 동일하게 유지 (바이너리 파싱 및 순수 데이터 로드용)
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
    }

    // 예시 코드: 어떻게 에셋을 불러올 것인가?
    /*
        using UnityEngine;
        using System.Threading.Tasks;
        using Script.Asset.Provider;

        /// <summary>
        /// [Framework] Manager 계층
        /// 인스턴스를 제어하고 흐름을 관리합니다.
        /// </summary>
        public class BattleManager : MonoBehaviour
        {
            // 1. Data-Driven 트랙 (수많은 몬스터 로드 시)
            public async Task SpawnMonsterAsync(MonsterData data)
            {
                // 엑셀에서 뽑아온 Addressable 문자열(data.PrefabAddress)을 바로 넘김! (Enum 없음)
                GameObject monsterObj = await AssetProvider.GetOrNewInstanceAsync(data.PrefabAddress);
        
                MonsterComponent monster = monsterObj.GetComponent<MonsterComponent>();
                monster.Initialize(data);
            }

            // 2. Type-Inference 트랙 (고유한 시스템 UI 로드 시)
            public async Task ShowBattleResultUIAsync()
            {
                // 프리팹 Addressable 주소가 "BattleResultUI"라고 가정.
                // 타입 <BattleResultUI>만 넘기면 찰떡같이 로드하고 컴포넌트까지 찾아줌!
                BattleResultUI resultUI = await AssetProvider.GetOrNewInstanceAsync<BattleResultUI>();
        
                resultUI.ShowResult();
            }
        }     
     */
}