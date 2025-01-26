namespace Script.Manager
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    /// <summary> 
    /// 이전 코드를 살린 내용이 많아서 리팩토링 필요 
    /// 어드레서블 에셋 생성/관리/삭제
    /// </summary>
    public static partial class AssetManager // Addressable assets
    {
        private static readonly Dictionary<int, AsyncOperationHandle>    objectHandlers = new Dictionary<int,    AsyncOperationHandle>();
        private static readonly Dictionary<string, AsyncOperationHandle> assetHandlers  = new Dictionary<string, AsyncOperationHandle>();

        // Init/Load Asset
        public static async Task<GameObject> InstantiateGameObjectAsync(string address, Transform parent, bool isOn)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent);
            GameObject go = await handle.Task;
            go.SetActive(isOn);

            objectHandlers.Add(go.GetInstanceID(), handle);
            return go;
        }
        private static Task<T> LoadAssetAsync<T>(string address)
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
            assetHandlers.Add(address, handle);
            return handle.Task;
        }

        // Spawn Unit
        public static async Task<T> SpawnUnit<T>(int index, Transform parent) where T : UnitBase, new()
        {
            string code = GetAssetAddress(EAssetType.Prefab, (int)EPrefabType.UnitBase);
            GameObject obj = await InstantiateGameObjectAsync(code, parent, true);

            T unit = new();
            unit.Awake(index, obj.transform);

            string address = GetAssetAddress(EAssetType.AnimCtrl, index);
            UnityEngine.Assertions.Assert.IsNotNull(address, "Can`t Find Asset Address: " + address);
            Task<RuntimeAnimatorController> taskController = LoadAssetAsync<RuntimeAnimatorController>(address);
            await taskController;

            UnityEngine.Assertions.Assert.IsNotNull(taskController.Result, "Can`t Find Asset Data: " + address);
            unit.SetAnimeController(taskController.Result);

            taskController.Dispose();
            return unit;
        }

        public static string GetAddress(EAsset code)
        {
            return code switch
            {
                /* unit */
                EAsset.UnitBase => "UnitBase",
                EAsset.AnimCtrl_Ataho => "AnimCtrl_Ataho",
                EAsset.AnimCtrl_Linxhang => "AnimCtrl_Linxhang",
                EAsset.AnimeCtrl_Smashu => "AnimeCtrl_Smashu",

                /* ui */
                EAsset.UITitle => "UITitle",

                /* content */
                EAsset.OpeningGame => "OpeningGame",

                /* default */
                _ => null,
            };
        }
        public static string GetAssetAddress(EAssetType type, int code)
        {
            int index = (byte)type * 10000 + code;

            // Visual Studio 2019의 추천에 따라봄 (switch 구문을 식으로 표시)
            return index switch
            {
                // Unit
                01_0000 => "AnimCtrl_Ataho",
                01_0001 => "AnimCtrl_Linxhang",
                01_0002 => "AnimeCtrl_Smashu",

                // Content
                02_0001 => "UnitBase",
                02_0002 => "OpeningGame",

                // UI
                03_0000 => "UITitle",

                // Default
                _ => null,
            };
        }

        // Release Asset
        public static void ClearAll()
        {
            foreach (AsyncOperationHandle handler in objectHandlers.Values)
            {
                GameObject.Destroy(handler.Result as GameObject);
                Addressables.Release(handler);
            }
            objectHandlers.Clear();

            foreach (string code in assetHandlers.Keys)
            {
                Addressables.Release(assetHandlers[code]);
            }
            assetHandlers.Clear();
        }
        public static bool ReleaseGameObject(int instanceID)
        {
            Addressables.Release(objectHandlers[instanceID]);
            return objectHandlers.Remove(instanceID);
        }
        public static bool ReleaseAsset(string code)
        {
            Addressables.Release(assetHandlers[code]);
            return assetHandlers.Remove(code);
        }
    }
}
