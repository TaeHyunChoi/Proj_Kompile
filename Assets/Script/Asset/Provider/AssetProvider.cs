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

    // 💡 [규칙 교정] 데이터 공급 및 캐싱 역할에 맞게 'AssetRepoProvider'로 명칭 변경
    public static partial class AssetProvider 
    {
        // 원본 데이터(Prefab) 캐싱 및 핸들 보관용 (GC 최소화)
        private static readonly Dictionary<AssetKey, GameObject> _prefabCache = new Dictionary<AssetKey, GameObject>();
        private static readonly Dictionary<AssetKey, AsyncOperationHandle<GameObject>> _prefabHandles = new Dictionary<AssetKey, AsyncOperationHandle<GameObject>>();

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
                    Debug.LogError($"[AssetRepoProvider Session] 커스텀 일괄 해제 중 예외 발생: {e.Message}");
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
                    if (nativeArray.IsCreated)
                    {
                        nativeArray.Dispose();
                    }
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
            Debug.LogError("[AssetRepoProvider] Addressable Key is null or empty!");
            return null;
        }

        public static async Awaitable<TEntity> GetOrNewEntityInstanceAsync<TEntity>(AssetKey assetKey, Transform root) where TEntity : UnitEntityBase
        {
            GameObject go = await GetOrNewInstanceAsync(assetKey, root);
            if (!go)
            {
                Debug.LogError("[AssetRepoProvider] 유닛 프리팹 로드 실패");
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

        // 💡 [완벽 적용] 원본 프리팹 비동기 로드를 전담하여 _prefabCache를 갱신합니다.
        public static async Awaitable<GameObject> GetOrLoadPrefabAsync(AssetKey key)
        {
            if (_prefabCache.TryGetValue(key, out GameObject prefab))
            {
                return prefab;
            }

            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key.Value);
            prefab = await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AssetRepoProvider] Failed to load prefab: {key.Value}");
                return null;
            }

            _prefabCache[key] = prefab;
            _prefabHandles[key] = handle;
            return prefab;
        }

        private static async Awaitable<GameObject> GetOrNewInstanceInternalAsync(AssetKey key, Transform parent, bool usePooling)
        {
            // 1. 프리팹 원본 데이터 확보 (캐시되어 있다면 즉시 통과)
            GameObject prefab = await GetOrLoadPrefabAsync(key);
            if (prefab == null) return null;

            if (!GameObjectInstances.TryGetValue(key, out InstanceEntryContext entry))
            {
                // 핸들은 _prefabHandles에서 가져와 Context에 위임합니다.
                AsyncOperationHandle<GameObject> handle = _prefabHandles[key];
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
                // 💡 [Zero-GC 달성] Addressables.InstantiateAsync를 버리고 메모리상 프리팹을 직접 복제
                instance = UnityEngine.Object.Instantiate(prefab, parent);
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
                // 직접 Instantiate한 객체이므로 ReleaseInstance 대신 Destroy로 파괴합니다.
                UnityEngine.Object.Destroy(instance);
            }

            entry.RemoveReference();
            if (!entry.ShouldRelease()) return;

            // 💡 프리팹 캐시 동반 해제
            _prefabCache.Remove(key);
            _prefabHandles.Remove(key);
            
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
                Debug.LogError($"[AssetRepoProvider] 에셋 로드 실패: {key.Value}");
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

        #region Animation Clips - Field & Runtime Override Integration
        private static readonly string[] DirectionNames = Enum.GetNames(typeof(EUnitAnimIndex));
        private static readonly int ClipCount = (int)EUnitAnimIndex.Count;
        private static readonly Dictionary<string, FieldUnitAnimClipContext> _fieldUnitAnimMap = new();
        private static readonly Dictionary<string, string[]> _addressCache = new(32);
        private static readonly Dictionary<string, Awaitable<FieldUnitAnimClipContext>> _loadingTasks = new(16);

        /// <summary> 런타임 오버라이드 시 GC Alloc을 원천 봉쇄하기 위한 내부 정적 캐시 리스트 </summary>
        private static readonly List<KeyValuePair<AnimationClip, AnimationClip>> _animBakeCache = new(32);

        public static async Awaitable<FieldUnitAnimClipContext> LoadFieldUnitAnimClipSetAsync(string unitKey)
        {
            if (_fieldUnitAnimMap.TryGetValue(unitKey, out FieldUnitAnimClipContext clipSet))
            {
                return clipSet;
            }

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

        /// <summary>
        /// [통합된 핵심 기능] 
        /// 컴포넌트가 요청한 인스턴스 컨트롤러에 가비지와 문자열 버그 없이 오버라이드 클립 세트를 주입합니다.
        /// </summary>
        public static void ApplyOverrideClips(AnimatorOverrideController runtimeAOC, in FieldUnitAnimClipContext clipSet)
        {
            if (runtimeAOC == null || clipSet.Clips == null) return;

            // 1. 내부 캐시 리스트를 재사용하여 가비지 생성 방지
            _animBakeCache.Clear();
            runtimeAOC.GetOverrides(_animBakeCache);

            int templateCount = _animBakeCache.Count;
            int clipCount = clipSet.Clips.Length;

            for (int i = 0; i < templateCount; ++i)
            {
                AnimationClip originalClip = _animBakeCache[i].Key;
                if (originalClip == null) continue;

                // 2. [버그 완벽 차단] 단순 Contains() 대신, 정립된 구조적 규칙 검사 수행
                for (int j = 0; j < clipCount; ++j)
                {
                    if (originalClip.name.EndsWith(DirectionNames[j], StringComparison.Ordinal))
                    {
                        _animBakeCache[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, clipSet.Clips[j]);
                        break;
                    }
                }
            }

            // 3. 일괄 적용 연산 호출
            runtimeAOC.ApplyOverrides(_animBakeCache);
        }

        /// <summary>
        /// [추가 권장] 세션 종료 전이라도, 특정 유닛이 더 이상 쓰이지 않을 때 관련 애니메이션 에셋만 메모리에서 정밀 해제합니다.
        /// </summary>
        public static void ReleaseUnitAnimClipSet(string unitKey)
        {
            // 1. 장부에서 데이터가 있는지 확인하고 추출
            if (!_fieldUnitAnimMap.Remove(unitKey, out FieldUnitAnimClipContext clipSet))
            {
                return;
            }

            if (clipSet.Clips == null) return;

            // 2. 해당 유닛이 가졌던 클립들을 순회하며 개별 핸들 해제
            for (int i = 0; i < clipSet.Clips.Length; i++)
            {
                if (clipSet.Clips[i] == null) continue;

                int id = clipSet.Clips[i].GetInstanceID();

                // NonGameObjectInstances 장부에서 핸들을 찾아 Release 수행
                if (NonGameObjectInstances.Remove(id, out var handle))
                {
                    if (handle.IsValid())
                    {
                        // 세션 추적 리스트에서도 해당 핸들을 찾아 제거 (중복 해제 방지)
                        if (_isSessionTrackingActive)
                        {
                            _sessionAnimHandles.Remove(handle);
                        }

                        Addressables.Release(handle);
                    }
                }
            }

            // 3. 세션 키 리스트에서도 제외
            if (_isSessionTrackingActive)
            {
                _sessionAnimUnitKeys.Remove(unitKey);
            }
        }
        #endregion
    }
}