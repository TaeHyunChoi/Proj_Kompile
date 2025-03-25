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
        public const string MAP_NAVI_DATA_PATH = "Rcs\\Bin\\MapNavRawData";

        private static readonly Dictionary<int, AsyncOperationHandle> assetHandlers  = new Dictionary<int, AsyncOperationHandle>();

        private static Transform[] canvasParents;
        private static UILoadingCurtainObject loadingCurtain;


        // Manage Binary File
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
        public static async Task<T> ReadBinaryFileAsync<T>(int targetGridKey)
        {
            string label = $"MapNavi_{targetGridKey}";

            // 어드레서블 에셋 로드
            AsyncOperationHandle<IList<TextAsset>> handler = Addressables.LoadAssetsAsync<TextAsset>(label, null);
            await handler.Task;

            if (handler.Status != AsyncOperationStatus.Succeeded || handler.Result.Count == 0)
            {
                throw new FileNotFoundException($"라벨에 해당하는 파일이 존재하지 않습니다: {label}");
            }

            // 파일에서 바이트 배열 읽기 및 역직렬화
            int instanceID = handler.Result[0].GetInstanceID();
            byte[] serializedData = handler.Result[0].bytes;
            T data = MessagePackSerializer.Deserialize<T>(serializedData);

            // 에셋 매니저에서 들고 있고.. 규칙이 이상한데?
            //AssetManager.AddHandler(instanceID, handler);

            // 자료 구했다는 값을 전달하고
            //MessageManager.Publish(Manager.MessageType.GET_ASSET, new OnGetAsset_MapGridData(Index.AssetCode.DB_MAP_GRID, data));

            //데이터의 instanceID를 넘겨야 탐색이 가능한가?


            return data;
        }


        //  Initialize/Cache UI, UI Canvas
        public static void Initialize(Transform mainTransform)
        {
            canvasParents = new Transform[2];
            Transform uiParent = mainTransform.GetChild(1);
            canvasParents[0] = uiParent.GetChild(0);
            canvasParents[1] = uiParent.GetChild(1);
            loadingCurtain = uiParent.GetChild(2).GetChild(0).GetComponentInChildren<UILoadingCurtainObject>();
        }


        // Instaniate, Load GameObject Assets
        public static async Task<GameObject> GetGameObjectAssetAsync(AssetCode assetCode, Transform parent, bool isOn)
        {
            GameObject targetObj;
            if (true == assetHandlers.TryGetValue((int)assetCode, out AsyncOperationHandle handler))
            {
                targetObj = (GameObject)handler.Result;
                targetObj.SetActive(isOn);
            }
            else
            {
                targetObj = await InstantiateGameObjectAsync(assetCode, parent, isOn);
            }

            MessageManager.Publish(MessageType.GET_ASSET, new OnGetAsset_GameObject(assetCode, targetObj));
            return targetObj;
        }
        private static async Task<GameObject> InstantiateGameObjectAsync(AssetCode assetCode, Transform parent, bool isOn)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(assetCode.ToString(), parent);
            GameObject go = await handle.Task;
            go.SetActive(isOn);

            assetHandlers.Add(go.GetInstanceID(), handle);
            return go;
        }


        // Get Cached UI, UI Canvas
        public static Transform GetCanvas(CanvasType type)
        {
            switch (type)
            {
                case CanvasType.CAMERA: return canvasParents[0];
                case CanvasType.OVERLAY: return canvasParents[1];
                default: return null;
            }
        }
        public static UILoadingCurtainObject GetLoadingCurtain()
        {
            return loadingCurtain;
        }
    }
}
