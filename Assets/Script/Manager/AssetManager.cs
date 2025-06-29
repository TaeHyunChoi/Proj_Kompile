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
    using System;
#endif

    public static partial class AssetManager
    {
        // TODO: 얘 날려야 함. MapGrid쪽 정리 요망
        private static readonly Dictionary<int, AsyncOperationHandle> assetHandles  = new Dictionary<int, AsyncOperationHandle>();

        private static Transform[] canvasParents;
        private static Transform mapRoot;
        private static Transform unitRoot;


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
        }


        // Table
#if UNITY_EDITOR
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
#endif
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

        public static async Task<IngameAsset_t> InstantiateGameObjectAsync(AssetCode assetCode, bool isOn)
        {
            Transform parent = GetIngameObjectParent(assetCode);
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(assetCode.ToString(), parent);
            GameObject targetObj = await handle.Task;
            targetObj.SetActive(isOn);

            return new IngameAsset_t(assetCode, handle);

            static Transform GetIngameObjectParent(AssetCode assetCode)
            {
                switch (assetCode)
                {
                    case AssetCode.UnitBase:
                        return unitRoot;

                    case AssetCode.MapGridPrefab:
                        return mapRoot;

                    case AssetCode.OP_TitleObject:
                    case AssetCode.UI_LoadingCurtain:
                    case AssetCode.UI_TitleMenuObject:
                        return canvasParents[(int)CanvasType.OVERLAY];
                    default:
                        break;
                }

                return null;
            }
        }
        public static async Task<(int, T)> LoadAssetAsync<T>(string key) where T : class
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            T value = await handle.Task;

            var hash_code = value.GetHashCode();
            assetHandles.Add(hash_code, handle);

            return (hash_code, value);
        }

        // Map
        private static async Task<(int, GameObject)> GetMapGridObjectPrefabAsync()
        {
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>("MapGridPrefab");
            await handle.Task;

            GameObject prefab = handle.Result;
            int instanceID = prefab.GetInstanceID();

            assetHandles.Add(instanceID, handle);

            return (instanceID, prefab);
        }
        private static async Task<(int, Mesh)[]> GetMapGridMeshesAsync(List<string> keys)
        {
            int length = keys.Count;

            AsyncOperationHandle<Mesh>[] meshTasks = new AsyncOperationHandle<Mesh>[length];
            Task[] tasks  = new Task[length];
            (int instanceID, Mesh mesh)[] result = new (int, Mesh)[length];

            for (int i = 0; i < length; ++i)
            {
                meshTasks[i] = Addressables.LoadAssetAsync<Mesh>(keys[i]);
                tasks[i]     = meshTasks[i].Task;
            }
            await Task.WhenAll(tasks);

            for (int i = 0; i < length; ++i)
            {
                result[i] = (meshTasks[i].Result.GetInstanceID(), meshTasks[i].Result);
                assetHandles.Add(result[i].instanceID, meshTasks[i]);
            }

            return result;
        }
        public static async Task<bool> InstaniateMapGrid(MapGridData data)
        {
            int length = data.assetFiles.Count;
            int[] mesh_instance_id = new int[length]; //여기에 mesh.instance_id도 함께 들고 있어야 해제가 가능한 거 아니오? 아니구나.. 그냥 mesh instance만 들고 있으면 되는건구나.

            (int prefab_id, GameObject prefab)      = await GetMapGridObjectPrefabAsync();
            (int instanceID, Mesh mesh)[] mesh_data = await GetMapGridMeshesAsync(data.assetFiles);

            // 여기서부터 unity main thread 로.
            GameObject obj;
            for (int i = 0; i < length; ++i)
            {
                obj = GameObject.Instantiate(prefab, mapRoot);
                obj.transform.position = Vector3.zero;
                obj.GetComponent<MeshFilter>().mesh = mesh_data[i].mesh;

                mesh_instance_id[i] = mesh_data[i].instanceID;
            }

            data.SetChildObjectMeshIDs(mesh_instance_id); // 메쉬 에셋을 해제할 때에 사용
            Dispose(prefab_id);
            return true;
        }
        public static async Task<MapGridData> LoadMapGridData(int gridKey)
        {
            string assetKey = $"MapNavi_{gridKey}";
            return await ReadBinaryFileAsync<MapGridData>(assetKey);
        }


        // Dispose
        public static void Dispose(int instanceID)
        {
            if (true == assetHandles.TryGetValue(instanceID, out var handle))
            {
                Addressables.ReleaseInstance(handle);
                assetHandles.Remove(instanceID);
            }
        }
    }
}
