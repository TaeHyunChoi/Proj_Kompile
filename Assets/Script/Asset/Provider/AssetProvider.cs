namespace Kompile.Asset.Provider
{
    using Kompile.Asset.Data;
    using Kompile.Unit.Data;
    using Kompile.Unit.Entity;
    using MessagePack;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using Unity.Collections;

    public static partial class AssetProvider
    {
        private static readonly Dictionary<AssetKey, InstanceEntryContext>
            GameObjectInstances = new Dictionary<AssetKey, InstanceEntryContext>();

        private static readonly Dictionary<int, AsyncOperationHandle>
            NonGameObjectInstances = new Dictionary<int, AsyncOperationHandle>();

        /// <summary> typeof(T).Name 호출 시 발생하는 string 할당(GC) 방지 캐시 </summary>
        private static class TypeNameCache<T> where T : Component
        {
            public static readonly string Name = typeof(T).Name;
        }

        #region 수명 주기 세션 자동 추적 시스템 (Optimized Version)
        private struct InstanceCleanupData
        {
            public AssetKey Key;
            public GameObject Instance;
            /// <summary> 이미 수동 해제된 인스턴스인지 판별하기 위한 고유 식별자 </summary>
            public int InstanceID;
        }

        // 각 타입별로 분할된 힙 할당 최소화용 장부 (GC 최소화 구조 유지)
        private static readonly List<InstanceCleanupData> _sessionInstances = new List<InstanceCleanupData>(128);
        private static readonly List<UnitEntityBase> _sessionEntities = new List<UnitEntityBase>(128);
        private static readonly List<int> _sessionAssetIds = new List<int>(128);
        private static readonly List<string> _sessionAnimUnitKeys = new List<string>(32);
        private static readonly List<AsyncOperationHandle> _sessionAnimHandles = new List<AsyncOperationHandle>(256);
        private static readonly List<Action> _sessionCustomActions = new List<Action>(16);

        private static bool _isSessionTrackingActive = false;
        private static bool _bypassSessionTracking = false;

        public static void BeginSession()
        {
            ClearAllSessionLedgers();
            _isSessionTrackingActive = true;
        }

        public static void EndAndReleaseSession()
        {
            _isSessionTrackingActive = false;

            // 1. 엔티티 데이터 청소 (생성의 역순)
            for (int i = _sessionEntities.Count - 1; i >= 0; i--)
            {
                if (_sessionEntities[i] != null) _sessionEntities[i].Clear();
            }

            // 2. 게임 오브젝트 인스턴스 해제
            for (int i = _sessionInstances.Count - 1; i >= 0; i--)
            {
                InstanceCleanupData data = _sessionInstances[i];

                // 이미 수동으로 해제되어 파괴되었거나 null이면 패스
                if (data.Instance == null) continue;

                // 풀링 시스템 연동을 위해, 이미 수동으로 풀에 반환된 객체인지 식별 검사
                if (!data.Instance.activeSelf && GameObjectInstances.TryGetValue(data.Key, out var entry))
                {
                    if (entry.IsAlreadyPooled(data.Instance)) continue;
                }

                ReleaseInstanceInternal(data.Key, data.Instance, false);
            }

            // 3. 순수 넌-게임오브젝트 에셋 해제
            for (int i = _sessionAssetIds.Count - 1; i >= 0; i--)
            {
                ReleaseAsset(_sessionAssetIds[i]);
            }

            // 4. 애니메이션 클립 에셋 및 매핑 해제
            for (int i = 0; i < _sessionAnimHandles.Count; i++)
            {
                if (_sessionAnimHandles[i].IsValid()) Addressables.Release(_sessionAnimHandles[i]);
            }
            for (int i = 0; i < _sessionAnimUnitKeys.Count; i++)
            {
                _fieldUnitAnimMap.Remove(_sessionAnimUnitKeys[i]);
            }

            // 5. NativeArray 및 커스텀 액션 등의 수동 등록 요소 해제 (안전한 예외 처리 포함)
            for (int i = _sessionCustomActions.Count - 1; i >= 0; i--)
            {
                try
                {
                    _sessionCustomActions[i]?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AssetProvider Session] 커스텀 일괄 해제 중 예외 발생: {e.Message}");
                }
            }

            ClearAllSessionLedgers();
        }

        private static void ClearAllSessionLedgers()
        {
            _sessionInstances.Clear();
            _sessionEntities.Clear();
            _sessionAssetIds.Clear();
            _sessionAnimUnitKeys.Clear();
            _sessionAnimHandles.Clear();
            _sessionCustomActions.Clear();
        }

        /// <summary> 매니저 내부에서 할당한 커스텀 해제 로직을 현재 세션 장부에 강제로 등록하고 싶을 때 사용 </summary>
        public static void RegisterToCurrentSession(Action cleanupAction)
        {
            if (_isSessionTrackingActive && cleanupAction != null)
            {
                _sessionCustomActions.Add(cleanupAction);
            }
        }

        /// <summary> 매니저 내부에서 할당한 고성능 NativeArray를 현재 세션 장부에 강제로 등록하고 싶을 때 사용 </summary>
        public static void RegisterToCurrentSession<T>(NativeArray<T> nativeArray) where T : struct
        {
            if (_isSessionTrackingActive)
            {
                _sessionCustomActions.Add(() =>
                {
                    if (nativeArray.IsCreated) nativeArray.Dispose();
                });
            }
        }
        #endregion

        #region Game Object (Instance & Pooling)
        public static async Awaitable<GameObject> GetOrNewInstanceAsync(AssetKey addressKey, Transform parent = null, bool usePooling = true)
        {
            if (addressKey.IsValid)
            {
                GameObject instance = await GetOrNewInstanceInternalAsync(addressKey, parent, usePooling);

                if (_isSessionTrackingActive && !_bypassSessionTracking && instance != null)
                {
                    _sessionInstances.Add(new InstanceCleanupData
                    {
                        Key = addressKey,
                        Instance = instance,
                        InstanceID = instance.GetInstanceID()
                    });
                }

                return instance;
            }
            Debug.LogError("[AssetProvider] Addressable Key is null or empty!");
            return null;
        }

        public static async Awaitable<TEntity> GetOrNewEntityInstanceAsync<TEntity>(AssetKey assetKey, Transform root) where TEntity : UnitEntityBase
        {
            GameObject go = await AssetProvider.GetOrNewInstanceAsync(assetKey, root);
            if (!go)
            {
                Debug.LogError("[AssetProvider] 유닛 프리팹 로드 실패");
                return null;
            }

            TEntity entity = go.AddComponent<TEntity>();
            entity.SetAssetKey(assetKey);

            if (_isSessionTrackingActive && !_bypassSessionTracking)
            {
                _sessionEntities.Add(entity);
            }
            return entity;
        }

        /// <summary> 제네릭 컴포넌트 타입 기반의 오브젝트 로드 복원 </summary>
        public static async Awaitable<T> GetOrNewInstanceAsync<T>(Transform parent = null, bool usePooling = true) where T : Component
        {
            AssetKey addressKey = new AssetKey(TypeNameCache<T>.Name);
            GameObject instance = await GetOrNewInstanceInternalAsync(addressKey, parent, usePooling);

            if (_isSessionTrackingActive && !_bypassSessionTracking && instance != null)
            {
                _sessionInstances.Add(new InstanceCleanupData
                {
                    Key = addressKey,
                    Instance = instance,
                    InstanceID = instance.GetInstanceID()
                });
            }

            return instance ? instance.GetComponent<T>() : null;
        }

        public static void ReleaseInstance(AssetKey addressKey, GameObject instance, bool forcedDestroy = false)
        {
            ReleaseInstanceInternal(addressKey, instance, forcedDestroy);
        }

        /// <summary> 제네릭 컴포넌트 타입 기반의 오브젝트 해제 복원 </summary>
        public static void ReleaseInstance<T>(GameObject instance, bool forcedDestroy = false) where T : Component
        {
            AssetKey addressKey = new AssetKey(TypeNameCache<T>.Name);
            ReleaseInstanceInternal(addressKey, instance, forcedDestroy);
        }

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
            if (instance == null) return;
            if (!key.IsValid || !GameObjectInstances.TryGetValue(key, out InstanceEntryContext entry))
            {
                Addressables.ReleaseInstance(instance);
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
            if (!entry.ShouldRelease()) return;

            Addressables.Release(entry.Handle);
            GameObjectInstances.Remove(key);
        }
        #endregion

        #region Non-GameObject Assets (Data Centric)
        /// <summary> 순수 에셋 로드 기능 복원 및 세션 자동 추적 연결 </summary>
        public static async Awaitable<T> LoadAssetAsync<T>(AssetKey key) where T : UnityEngine.Object
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key.Value);
            T result = await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AssetProvider] 에셋 로드 실패: {key.Value}");
                return null;
            }

            int id = result.GetInstanceID();
            NonGameObjectInstances.TryAdd(id, handle);

            if (_isSessionTrackingActive && !_bypassSessionTracking)
            {
                _sessionAssetIds.Add(id);
            }

            return result;
        }

        public static void ReleaseAsset(int instanceID)
        {
            if (NonGameObjectInstances.Remove(instanceID, out var handle))
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }

        /// <summary> MessagePack 바이너리 데이터 파일 읽기 로직 복원 </summary>
        public static async Awaitable<T> ReadBinaryDataAsync<T>(string assetKey)
        {
            AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(assetKey);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                TextAsset textAsset = handle.Result;
                if (textAsset && textAsset.bytes != null)
                {
                    try
                    {
                        T deserializedData = MessagePackSerializer.Deserialize<T>(textAsset.bytes);
                        Addressables.Release(handle);
                        return deserializedData;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[MessagePack 디버그 에러] {ex.Message}");
                        Addressables.Release(handle);
                        throw;
                    }
                }
            }

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            return default;
        }
        #endregion

        #region Animation Clips - Field (Race Condition Fixed)
        private static readonly string[] DirectionNames = Enum.GetNames(typeof(EUnitAnimIndex));
        private static readonly int ClipCount = (int)EUnitAnimIndex.Count;
        private static readonly Dictionary<string, FieldUnitAnimClipContext> _fieldUnitAnimMap = new();
        private static readonly Dictionary<string, string[]> _addressCache = new(32);

        /// <summary> 동시 프레임 로딩 경쟁을 막기 위한 진행 중인 비동기 태스크 전용 장부 </summary>
        private static readonly Dictionary<string, Awaitable<FieldUnitAnimClipContext>> _loadingTasks = new(16);

        public static async Awaitable<FieldUnitAnimClipContext> LoadFieldUnitAnimClipSetAsync(string unitKey)
        {
            if (_fieldUnitAnimMap.TryGetValue(unitKey, out FieldUnitAnimClipContext clipSet))
            {
                return clipSet;
            }

            // 중복 비동기 호출 시 레이스 컨디션 방지 (먼저 돌고 있는 태스크를 캐시하여 대기)
            if (_loadingTasks.TryGetValue(unitKey, out Awaitable<FieldUnitAnimClipContext> ongoingTask))
            {
                return await ongoingTask;
            }

            Awaitable<FieldUnitAnimClipContext> loadTask = ExecuteLoadAnimClipSetInternalAsync(unitKey);
            _loadingTasks.Add(unitKey, loadTask);

            try
            {
                clipSet = await loadTask;
            }
            finally
            {
                _loadingTasks.Remove(unitKey);
            }

            return clipSet;
        }

        private static async Awaitable<FieldUnitAnimClipContext> ExecuteLoadAnimClipSetInternalAsync(string unitKey)
        {
            if (!_addressCache.TryGetValue(unitKey, out string[] cachedAddresses))
            {
                cachedAddresses = new string[ClipCount];
                for (int j = 0; j < ClipCount; j++)
                {
                    cachedAddresses[j] = $"{unitKey}_{DirectionNames[j]}";
                }
                _addressCache.Add(unitKey, cachedAddresses);
            }

            AnimationClip[] clips = new AnimationClip[ClipCount];
            var handles = new AsyncOperationHandle<AnimationClip>[ClipCount];

            for (int j = 0; j < ClipCount; j++)
            {
                handles[j] = Addressables.LoadAssetAsync<AnimationClip>(cachedAddresses[j]);
            }

            for (int j = 0; j < ClipCount; j++)
            {
                await handles[j].Task;
                if (handles[j].Status == AsyncOperationStatus.Succeeded)
                {
                    clips[j] = handles[j].Result;
                    NonGameObjectInstances.TryAdd(handles[j].Result.GetInstanceID(), handles[j]);
                }
                else
                {
                    Debug.LogError($"[AnimLoader] 에셋 로드 실패 주소 체크: {cachedAddresses[j]}");
                }
            }

            var clipSet = new FieldUnitAnimClipContext { UnitKey = unitKey, Clips = clips };
            _fieldUnitAnimMap[unitKey] = clipSet;

            if (_isSessionTrackingActive && !_bypassSessionTracking)
            {
                _sessionAnimUnitKeys.Add(unitKey);
                for (int i = 0; i < ClipCount; i++)
                {
                    _sessionAnimHandles.Add(handles[i]);
                }
            }

            return clipSet;
        }

        public static async Awaitable PreloadFieldUnitAnimsAsync(string[] unitKeys)
        {
            _bypassSessionTracking = true;
            try
            {
                for (int i = 0; i < unitKeys.Length; i++)
                {
                    string key = unitKeys[i];
                    if (_fieldUnitAnimMap.ContainsKey(key)) continue;

                    await LoadFieldUnitAnimClipSetAsync(key);
                }
            }
            finally
            {
                _bypassSessionTracking = false;
            }
        }
        #endregion
    }
}