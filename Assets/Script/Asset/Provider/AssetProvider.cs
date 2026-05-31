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

        #region 수명 주기 세션 자동 추적 시스템 (Session Automation)
        /// <summary> 현재 활성화된 세션 내에서 해제해야 할 액션들을 모으는 리스트<br/>
        /// 만약 필드가 엄청나게 넓어서 중간에 잠깐 쓰고 버려야 하는 임시 에셋이 있다면,
        /// 세션 자동 등록에 맡기지 말고 기존처럼 AssetProvider.ReleaseInstance를 통해 수동으로 즉시 해제해 주어야 메모리 정점(Peak)을 낮출 수 있습니다.
        /// </summary>
        private static readonly List<Action> _sessionCleanupActions = new List<Action>();
        private static bool _isSessionTrackingActive = false;

        /// <summary> 리소스 자동 추적 세션을 시작 (게임 초기화 시점에 호출) </summary>
        public static void BeginSession()
        {
            _sessionCleanupActions.Clear();
            _isSessionTrackingActive = true;
        }

        /// <summary> 다른 매니저의 OnDisable() 등에서 호출하여, 세션 동안 수집된 모든 에셋/인스턴스/네이티브 배열을 역순으로 일괄 해제 </summary>
        public static void EndAndReleaseSession()
        {
            _isSessionTrackingActive = false;

            // 생성의 역순으로 안전하게 해제 처리
            for (int i = _sessionCleanupActions.Count - 1; i >= 0; i--)
            {
                try
                {
                    _sessionCleanupActions[i]?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AssetProvider Session] 일괄 해제 중 예외 발생: {e.Message}");
                }
            }
            _sessionCleanupActions.Clear();
        }

        /// <summary> 매니저 내부에서 할당한 고성능 NativeArray나 커스텀 해제 로직을 현재 세션 장부에 강제로 등록하고 싶을 때 사용 </summary>
        public static void RegisterToCurrentSession(Action cleanupAction)
        {
            if (_isSessionTrackingActive && cleanupAction != null)
            {
                _sessionCleanupActions.Add(cleanupAction);
            }
        }
        
        /// <summary> 매니저 내부에서 할당한 고성능 NativeArray나 커스텀 해제 로직을 현재 세션 장부에 강제로 등록하고 싶을 때 사용 </summary>
        public static void RegisterToCurrentSession<T>(NativeArray<T> nativeArray) where T : struct
        {
            if (_isSessionTrackingActive)
            {
                _sessionCleanupActions.Add(() =>
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
                
                // [자동화] 세션이 켜져 있다면 파괴 장부에 인스턴스 해제 예약 등록
                if (_isSessionTrackingActive && instance != null)
                {
                    _sessionCleanupActions.Add(() => ReleaseInstanceInternal(addressKey, instance, false));
                }
                
                return instance;
            }

            Debug.LogError("[AssetProvider] Addressable Key is null or empty!");
            return null;
        }

        public static async Awaitable<TEntity> GetOrNewEntityInstanceAsync<TEntity>(AssetKey assetKey, Transform root)
            where TEntity : UnitEntityBase
        {
            GameObject go = await AssetProvider.GetOrNewInstanceAsync(assetKey, root);
            if (!go)
            {
                Debug.LogError("[AssetProvider] 유닛 프리팹 로드 실패");
                return null;
            }

            TEntity entity = go.AddComponent<TEntity>();
            entity.SetAssetKey(assetKey);

            // [자동화] 엔티티 내부의 데이터 청소(Clear) 로직도 세션 해제 시 함께 연쇄 반응하도록 등록
            if (_isSessionTrackingActive)
            {
                _sessionCleanupActions.Add(() => { if (entity != null) entity.Clear(); });
            }

            return entity;
        }

        public static void ReleaseInstance(AssetKey addressKey, GameObject instance, bool forcedDestroy = false)
        {
            ReleaseInstanceInternal(addressKey, instance, forcedDestroy);
        }

        public static async Awaitable<T> GetOrNewInstanceAsync<T>(Transform parent = null, bool usePooling = true) where T : Component
        {
            AssetKey addressKey = new AssetKey(TypeNameCache<T>.Name);
            GameObject instance = await GetOrNewInstanceInternalAsync(addressKey, parent, usePooling);

            if (_isSessionTrackingActive && instance != null)
            {
                _sessionCleanupActions.Add(() => ReleaseInstanceInternal(addressKey, instance, false));
            }

            return instance ? instance.GetComponent<T>() : null;
        }

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

            // [자동화] 순수 에셋 로드 시 세션에 자동 등록
            if (_isSessionTrackingActive)
            {
                int id = result.GetInstanceID();
                _sessionCleanupActions.Add(() => ReleaseAsset(id));
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

        #region Animation Clips - Field
        private static readonly string[] DirectionNames = Enum.GetNames(typeof(EUnitAnimIndex));
        private static readonly int ClipCount = (int)EUnitAnimIndex.Count;
        private static readonly Dictionary<string, FieldUnitAnimClipContext> _fieldUnitAnimMap = new();

        public static async Awaitable<FieldUnitAnimClipContext> LoadFieldUnitAnimClipSetAsync(string unitKey)
        {
            if (_fieldUnitAnimMap.TryGetValue(unitKey, out FieldUnitAnimClipContext clipSet))
            {
                return clipSet;
            }

            string targetLabel = $"Anim_{unitKey}";
            AnimationClip[] clips = new AnimationClip[ClipCount];
            AsyncOperationHandle<IList<AnimationClip>> handle = Addressables.LoadAssetsAsync<AnimationClip>(targetLabel, null);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                IList<AnimationClip> loadedClips = handle.Result;
                int loadedCount = loadedClips.Count;

                for (int i = 0; i < loadedCount; i++)
                {
                    AnimationClip clip = loadedClips[i];
                    if (clip == null) continue;

                    for (int j = 0; j < ClipCount; j++)
                    {
                        if (clip.name.Contains(DirectionNames[j], StringComparison.OrdinalIgnoreCase))
                        {
                            clips[j] = clip;
                            break;
                        }
                    }

                    NonGameObjectInstances.TryAdd(clip.GetInstanceID(), handle);
                    
                    // [자동화] 라벨로 긁어온 개별 클립들도 세션 자동 타깃 지정
                    if (_isSessionTrackingActive)
                    {
                        int id = clip.GetInstanceID();
                        _sessionCleanupActions.Add(() => ReleaseAsset(id));
                    }
                }
            }

            clipSet = new FieldUnitAnimClipContext
            {
                UnitKey = unitKey,
                Clips = clips
            };

            _fieldUnitAnimMap[unitKey] = clipSet;

            // 세션 종료 시 글로벌 맵에서도 청소되도록 매핑 해제 등록
            if (_isSessionTrackingActive)
            {
                _sessionCleanupActions.Add(() => _fieldUnitAnimMap.Remove(unitKey));
            }

            return clipSet;
        }

        public static async Awaitable PreloadFieldUnitAnimsAsync(string[] unitKeys)
        {
            for (int i = 0; i < unitKeys.Length; i++)
            {
                string key = unitKeys[i];
                if (_fieldUnitAnimMap.ContainsKey(key)) continue;

                await LoadFieldUnitAnimClipSetAsync(key);
            }
        }
        #endregion
    }
}