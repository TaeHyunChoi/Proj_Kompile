namespace Script.Manager
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using Script.Index;
    using MessagePack;
    using Script.Data;
    using System.IO;

#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
#endif
    
    public static partial class AssetManager
    {
        // Map?
        public static async Task<MapGridData> InstaniateMapGrid(int gridKey)
        {
            MapGridData data = await ReadBinaryFileAsync<MapGridData>($"MapNavi_{gridKey}");

            List<int> mesh_instance_id = new List<int>();

            // 하.. 여기서 Mesh 꺼내는 것도 좋은데.. instanceID가 필요한가 싶다. 
            // layer_index가 필요한 것 같은데.

            // 아래 2개를 합치는 게 맞을 것 같은데
            // 이 부분 정리 필요
            List<(int instanceID, Mesh mesh)> mesh_data = await GetMapGridMeshesAsync(data.layer_table);

            GameObject obj;
            for (int i = 0; i < mesh_data.Count; ++i)
            {
                obj = await GetOrNewInstanceAsync(AssetCode.MapGridPrefab, AssetParentType.MAP_ROOT);
                obj.transform.position = Vector3.zero;
                obj.GetComponent<MeshFilter>().mesh = mesh_data[i].mesh;

                // 생성 뭐시기에 (int layer, GameObject obj)를 묶어서 처리하던가 그래야 할 것 같은데요
                // 그렇다면?... 

                mesh_instance_id.Add(mesh_data[i].instanceID);
                await Task.Yield();
            }

            data.SetChildObjectMeshIDs(mesh_instance_id.ToArray());
            return data;
        }
        public static async Task<List<(int, Mesh)>> GetMapGridMeshesAsync(List<GridLayerData> layer_data)
        {
            int length = layer_data.Count;

            List<AsyncOperationHandle<Mesh>> meshTasks = new List<AsyncOperationHandle<Mesh>>(length);
            List<Task> tasks  = new List<Task>(length);
            List<(int instanceID, Mesh mesh)> result = new List<(int, Mesh)>(length);

            for (int i = 0; i < length; ++i)
            {
                for (int j = 0; j < layer_data[i].assets.Count; ++j)
                {
                    AsyncOperationHandle<Mesh> handle = Addressables.LoadAssetAsync<Mesh>(layer_data[i].assets[j]);
                    meshTasks.Add(handle);
                    tasks.Add(handle.Task);
                }
            }
            await Task.WhenAll(tasks);

            for (int i = 0; i < meshTasks.Count; ++i)
            {
                result.Add((meshTasks[i].Result.GetInstanceID(), meshTasks[i].Result));
                _nonGameObjectInstances.TryAdd(result[i].instanceID, meshTasks[i]);
            }

            return result;
        }
        public static async Task<Mesh> GetMeshAssetAsync(string address)
        {
            AsyncOperationHandle<Mesh> handle = Addressables.LoadAssetAsync<Mesh>(address);
            await handle.Task;

            Mesh mesh = handle.Result;
            _nonGameObjectInstances.TryAdd(mesh.GetInstanceID(), handle);

            return mesh;
        }
    }

    public static partial class AssetManager
    {
        private static readonly ConcurrentDictionary<string, InstanceEntry>     _gameObjectInstances    = new ConcurrentDictionary<string, InstanceEntry>(); // 풀링까지 고려
        private static readonly ConcurrentDictionary<int, AsyncOperationHandle> _nonGameObjectInstances = new ConcurrentDictionary<int, AsyncOperationHandle>();

        private static Transform[] canvasParents;
        private static Transform mapRoot;
        private static Transform unitRoot;

        private static System.Threading.SynchronizationContext mainSyncContext;

        // Initialize
        public static void Initialize(Transform mainTransform)
        {
            canvasParents = new Transform[3];
            Transform uiParent = mainTransform.Find("UI");
            for (int i = 0; i < canvasParents.Length; ++i)
            {
                canvasParents[i] = uiParent.GetChild(i);
            }

            mapRoot = mainTransform.Find("Map").transform;
            unitRoot = mainTransform.Find("Unit").transform;

            mainSyncContext = System.Threading.SynchronizationContext.Current;
        }


        // Table Data (Binary)
        public static async Task<T> ReadBinaryFileAsync<T>(string key)
        {
            // 어드레서블 에셋 로드
            AsyncOperationHandle<IList<TextAsset>> handle = Addressables.LoadAssetsAsync<TextAsset>(key, null);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result.Count == 0)
            {
                throw new FileNotFoundException($"라벨에 해당하는 파일이 존재하지 않습니다: {key}");
            }

            // 파일에서 바이트 배열 읽기 및 역직렬화
            byte[] serializedData = handle.Result[0].bytes;
            T data = MessagePackSerializer.Deserialize<T>(serializedData);

            // T라는 데이터로 저장했으니 원본 TextAsset은 가지고 있을 이유가 없다. -> 곧장 해제
            handle.ReleaseHandleOnCompletion();
            return data;
        }
        public static async Task<MapGridData> LoadMapGridBinaryData(int gridKey)
        {
            string assetKey = $"MapNavi_{gridKey}";
            return await ReadBinaryFileAsync<MapGridData>(assetKey);
        }

        // GameObject Instance
        public static async Task<GameObject> GetOrNewInstanceAsync(AssetCode assetCode, AssetParentType parentType, bool usePooling = false)
        {
            string key = assetCode.ToString();

            if (false == _gameObjectInstances.TryGetValue(key, out InstanceEntry entry))
            {
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    var obj = handle.Result;
                    var hash_code = obj.GetHashCode();
                    _nonGameObjectInstances.TryAdd(hash_code, handle);
                    Debug.Log($"[AssetManager] Successfully loaded asset: {key}");
                }
                else
                {
                    Debug.LogError($"[AssetManager] Failed to load asset '{key}'. Status: {handle.Status}, Exception: {handle.OperationException}");
                    throw new System.Exception($"Failed to load asset: {key}. Error: {handle.OperationException}");
                }

                entry = new InstanceEntry(handle, usePooling);
                _gameObjectInstances.TryAdd(key, entry);
            }

            GameObject instance;

            if (true == entry.HasPooledInstance())
            {
                instance = entry.Pool.Dequeue();
                instance.SetActive(true);
            }
            else
            {
                Transform parent = GetIngameObjectParent(parentType);
                AsyncOperationHandle<GameObject> instHandle = Addressables.InstantiateAsync(key, parent);
                instance = await instHandle.Task;
            }

            entry.AddReference();
            return instance;
        }

        private static Transform GetIngameObjectParent(AssetParentType parentType)
        {
            switch (parentType)
            {
                case AssetParentType.UNIT_ROOT:
                    return unitRoot;
                case AssetParentType.MAP_ROOT:
                    return mapRoot;
                case AssetParentType.CANVAS_CAMERA:
                    return canvasParents[(int)CanvasType.CAMERA];
                case AssetParentType.CANVAS_OVERAY:
                    return canvasParents[(int)CanvasType.OVERLAY];
                default:
                    break;
            }

            return null;
        }
        public static void ReleaseInstance(AssetCode assetCode, GameObject instance)
        {
            string key = assetCode.ToString();
            if (false == _gameObjectInstances.TryGetValue(key, out InstanceEntry entry))
            {
                return;
            }

            if (true == entry.UsePooling)
            //&& false == entry.Pool.Contains(instance))
            {
                instance.SetActive(false);
                entry.Pool.Enqueue(instance);
            }
            else
            {
                Addressables.ReleaseInstance(instance);
            }

            entry.RemoveReference();

            if (true == entry.ShouldRelease())
            {
                Addressables.Release(entry.Handle);
                _gameObjectInstances.TryRemove(key);

#if UNITY_EDITOR
                Debug.Log($"[AssetManager] Release[{assetCode}]");
#endif
            }
        }


        // Non GameObject Assets (animation, mesh, ...)
        public static async Task<(int HashCode, T Value)> LoadAssetAsync<T>(string key) where T : class
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
            T value = await handle.Task;

            var hash_code = value.GetHashCode();
            _nonGameObjectInstances.TryAdd(hash_code, handle);

            return (hash_code, value);
        }
        public static void Dispose(int instanceID)
        {
            mainSyncContext.Post((state) =>
            {
                if (_nonGameObjectInstances.TryGetValue(instanceID, out var handle))
                {
                    Addressables.ReleaseInstance(handle);
                    _nonGameObjectInstances.TryRemove(instanceID);
                }
            },
            null);
        }
    }


#if UNITY_EDITOR
    public static partial class AssetManager
    {
        public static void WriteBinaryFile<T>(T data, string dataPath, string fileName, string addressableGroup = null)
        {
            // 저장할 파일 경로 생성
            string filePath = Path.Combine(Application.dataPath, dataPath, fileName + ".bytes");
            string directoryPath = Path.GetDirectoryName(filePath);
            if (false == Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // 데이터를 MessagePack 형식으로 직렬화하고 파일에 저장
            byte[] serializedData = MessagePackSerializer.Serialize(data, MessagePackConfig<T>.Options);
            File.WriteAllBytes(filePath, serializedData);

            // 어드레서블 에셋으로 저장
            if (null != addressableGroup)
            {
                string assetPath = "Assets/" + dataPath + "/" + fileName + ".bytes";
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                AddressableAssetGroup group = settings.FindGroup(addressableGroup);
                if (group == null)
                {
                    group = settings.CreateGroup(addressableGroup, false, false, false, null);
                }

                AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), group, readOnly: true);
                entry.SetAddress(fileName);
                entry.SetLabel(fileName, true);
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                AssetDatabase.SaveAssets();
            }
        }
    }
#endif
}
