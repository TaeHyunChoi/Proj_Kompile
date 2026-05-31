namespace Kompile.Asset.Provider
{
    using Kompile.Asset.Data;
    using Kompile.Asset.Utility;
    using Kompile.Unit.Data;
    using Kompile.Unit.Entity;
    using MessagePack;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceLocations;
    
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

        public static async Awaitable<T> ReadBinaryDataAsync<T>(string assetKey)
        {
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<TextAsset>(assetKey);
            await handle.Task;

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                TextAsset textAsset = handle.Result;
                if (textAsset != null && textAsset.bytes != null)
                {
                    try
                    {
                        // 실제 파싱 시도
                        T deserializedData = MessagePackSerializer.Deserialize<T>(textAsset.bytes);
                        UnityEngine.AddressableAssets.Addressables.Release(handle);
                        return deserializedData;
                    }
                    catch (System.Exception ex)
                    {
                        // 콘솔창에 진짜 범인을 출력합니다.
                        Debug.LogError($"[MessagePack 디버그 에러] {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Debug.LogError($"[MessagePack 상세 원인] {ex.InnerException.Message}");
                        }
                        UnityEngine.AddressableAssets.Addressables.Release(handle);
                        throw;
                    }
                }
            }

            if (handle.IsValid()) UnityEngine.AddressableAssets.Addressables.Release(handle);
            return default;
        }

        //public static async Awaitable<T> ReadBinaryDataAsync<T>(string key)
        //{
        //    // ★ 복구됨: Addressables 예외(Exception)를 막기 위한 안전장치.
        //    // Manager의 블랙리스트 처리 덕분에 없는 키에 대해서는 단 1회만 호출되므로 최적화가 보장됩니다.
        //    AsyncOperationHandle<IList<IResourceLocation>> locHandle = Addressables.LoadResourceLocationsAsync(key);
        //    IList<IResourceLocation> locations = await locHandle.Task;

        //    if (locations == null 
        //        || locations.Count == 0)
        //    {
        //        if (locHandle.IsValid())
        //        {
        //            Addressables.Release(locHandle);                    
        //        }

        //        return default; // null 반환 시 Manager가 블랙리스트에 등록함
        //    }

        //    if (locHandle.IsValid())
        //    {
        //        Addressables.Release(locHandle);                
        //    }

        //    // 2. 실제 에셋 로드 (안전하게 호출됨)
        //    AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(key);
        //    TextAsset textAsset = await handle.Task;

        //    try
        //    {
        //        if (handle.Status != AsyncOperationStatus.Succeeded || !textAsset)
        //        {
        //            return default;
        //        }

        //        // 정적으로 캐싱된 옵션 사용
        //        return MessagePackSerializer.Deserialize<T>(textAsset.bytes, MsgPackOptions);
        //    }
        //    finally
        //    {
        //        if (handle.IsValid())
        //        {
        //            Addressables.Release(handle);                    
        //        }
        //    }
        //}

        #endregion

        #region Animation Clips - Field
        // Enum 문자열 배열 및 크기 캐싱 (GC 방지)
        private static readonly string[] DirectionNames = Enum.GetNames(typeof(EUnitAnimIndex));
        private static readonly int ClipCount = (int)EUnitAnimIndex.Count;

        // 컨텍스트 데이터 보관용 맵
        private static readonly Dictionary<string, FieldUnitAnimClipContext> _fieldUnitAnimMap = new();

        public static async Awaitable<FieldUnitAnimClipContext> LoadFieldUnitAnimClipSetAsync(string unitKey)
        {
            if (_fieldUnitAnimMap.TryGetValue(unitKey, out FieldUnitAnimClipContext clipSet))
            {
                return clipSet;
            }

            // 라벨 하나를 던져 연관된 8방향 에셋을 단 한 번의 쿼리로 동시 로드
            string targetLabel = $"Anim_{unitKey}";
            AnimationClip[] clips = new AnimationClip[ClipCount];
            AsyncOperationHandle<IList<AnimationClip>> handle = Addressables.LoadAssetsAsync<AnimationClip>(targetLabel, null);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                IList<AnimationClip> loadedClips = handle.Result;
                int loadedCount = loadedClips.Count;

                // [중요] 라벨 로드는 순서가 무작위이므로, 이름을 비교하여 올바른 배열 인덱스에 배치
                for (int i = 0; i < loadedCount; i++)
                {
                    AnimationClip clip = loadedClips[i];
                    if (clip == null)
                    {
                        continue;
                    }

                    // 캐싱된 방향 이름을 순회하며 매핑
                    for (int j = 0; j < ClipCount; j++)
                    {
                        if (clip.name.Contains(DirectionNames[j], StringComparison.OrdinalIgnoreCase))
                        {
                            clips[j] = clip;
                            break;
                        }
                    }

                    // 나으리의 프레임워크 규칙에 따른 핸들 인스턴스 등록
                    NonGameObjectInstances.TryAdd(clip.GetInstanceID(), handle);
                }
            }
            else
            {
                Debug.LogError($"[Loader] 라벨 로드 실패. 유닛 에셋에 라벨이 누락되었는지 확인하세요: {targetLabel}");
            }

            // 4. 가치 중심(Value-Centric) 컨텍스트 구조체 완성
            clipSet = new FieldUnitAnimClipContext
            {
                UnitKey = unitKey,
                Clips = clips
            };

            _fieldUnitAnimMap[unitKey] = clipSet;
            return clipSet;
        }

        // -------------- v2 -------------
        //private static Dictionary<string, FieldUnitAnimClipContext> _fieldUnitAnimMap = new(64);
        //private static string[] _directionNames = Enum.GetNames(typeof(EUnitAnimIndex));

        //public static async Awaitable<FieldUnitAnimClipContext> LoadFieldUnitAnimClipSetAsync(string unitKey)
        //{
        //    if (_fieldUnitAnimMap.TryGetValue(unitKey, out FieldUnitAnimClipContext clipSet))
        //    {
        //        return clipSet;
        //    }

        //    // 2. 8방향 규칙에 맞춰 비동기 로드 예약
        //    int clipCount = (int)EUnitAnimIndex.Count;
        //    AnimationClip[] clips = new AnimationClip[clipCount];
        //    AsyncOperationHandle<AnimationClip>[] handles = new AsyncOperationHandle<AnimationClip>[clipCount];

        //    for (int i = 0; i < clipCount; ++i)
        //    {
        //        if (i == clipCount - 1) 
        //            break;

        //        handles[i] = Addressables.LoadAssetAsync<AnimationClip>($"{unitKey}_{_directionNames[i]}");
        //    }

        //    for (int i = 0; i < handles.Length; i++)
        //    {
        //        if (!handles[i].IsValid())
        //        {
        //            continue;
        //        }

        //        await handles[i].Task;
        //        clips[i] = handles[i].Result;

        //        NonGameObjectInstances.TryAdd(handles[i].Result.GetInstanceID(), handles[i]);
        //    }

        //    // 4. 컨텍스트 데이터 완성 (Value-Centric)
        //    clipSet = new FieldUnitAnimClipContext
        //    {
        //        UnitKey = unitKey,
        //        Clips = clips
        //    };

        //    _fieldUnitAnimMap.Add(unitKey, clipSet);
        //    return clipSet;
        //}

        // -------------- v1 ---------------------
        //private static Dictionary<string, FieldUnitAnimClipContext> _fieldUnitAnimMap = new(128);

        //public static bool TryGetFieldUnitAnim(string unitKey, out FieldUnitAnimClipContext clipSet)
        //{
        //    return _fieldUnitAnimMap.TryGetValue(unitKey, out clipSet);
        //}

        //public static async Awaitable<FieldUnitAnimClipContext> LoadFieldUnitAnimClipSetAsync(string unitKey)
        //{
        //    if (_fieldUnitAnimMap.TryGetValue(unitKey, out FieldUnitAnimClipContext clipSet))
        //    {
        //        return clipSet;
        //    }

        //    // 이름 규칙과 순서를 고정했다고 가정;
        //    var idleHandle = Addressables.LoadAssetAsync<AnimationClip>($"{unitKey}_Idle");
        //    var walkHandle = Addressables.LoadAssetAsync<AnimationClip>($"{unitKey}_Walk");

        //    await idleHandle.Task;
        //    await walkHandle.Task;

        //    AnimationClip[] clips = new AnimationClip[2]
        //    {
        //        idleHandle.Result,
        //        walkHandle.Result
        //    };

        //    clipSet = new FieldUnitAnimClipContext
        //    {
        //        UnitKey = unitKey,
        //        Clips = clips
        //    };

        //    NonGameObjectInstances.TryAdd(idleHandle.Result.GetInstanceID(), idleHandle);
        //    NonGameObjectInstances.TryAdd(walkHandle.Result.GetInstanceID(), walkHandle);

        //    _fieldUnitAnimMap.Add(unitKey, clipSet);

        //    return clipSet;
        //}

        /// <summary> 필드 진입 시 매니저가 호출하는 전제 조건 바인딩.
        /// 해당 필드에 등장할 유닛 목록을 배열(값 집합)로 받아 가비지 없이 한 번에 예전(Preload)합니다.
        /// </summary>
        public static async Awaitable PreloadFieldUnitAnimsAsync(string[] unitKeys)
        {
            for (int i = 0; i < unitKeys.Length; i++)
            {
                string key = unitKeys[i];
                if (_fieldUnitAnimMap.ContainsKey(key))
                {
                    continue;
                }

                await LoadFieldUnitAnimClipSetAsync(key);
            }
        }

        ///// <summary> 필드 탈출 시 내부 버퍼를 보존하며 가비지 없이 장부를 청소 </summary>
        ////public static void ClearFieldUnitAnimMap()
        ////{
        ////    _fieldUnitAnimMap.Clear();
        ////}
        #endregion
    }
}