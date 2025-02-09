namespace Script.Manager
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using Script.Index;

    /// <summary> 
    /// 이전 코드를 살린 내용이 많아서 리팩토링 필요 
    /// 어드레서블 에셋 생성/관리/삭제
    /// </summary>
    public static partial class AssetManager // Addressable assets
    {
        private static readonly Dictionary<int, AsyncOperationHandle> assetHandlers  = new Dictionary<int, AsyncOperationHandle>();

        // Instaniate, Load GameObject Assets
        public static async Task<GameObject> GetGameObjectAssetAsync(EAssetName asset, Transform parent, bool isOn)
        {
            if (true == assetHandlers.TryGetValue((int)asset, out AsyncOperationHandle handler))
            {
                GameObject obj = (GameObject)handler.Result;
                obj.SetActive(isOn);

                return obj;
            }

            return await InstantiateGameObjectAsync(asset, parent, isOn);
        }
        private static async Task<GameObject> InstantiateGameObjectAsync(EAssetName asset, Transform parent, bool isOn)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(asset.ToString(), parent);
            GameObject go = await handle.Task;
            go.SetActive(isOn);

            assetHandlers.Add(go.GetInstanceID(), handle);
            return go;
        }

        // Load Non-GameObject Assets
        public static async Task<T> GetAssetAsync<T>(EAssetName assetName)
        {
            if (true == assetHandlers.TryGetValue((int)assetName, out AsyncOperationHandle handler))
            {
                return (T)handler.Result;
            }

            return await LoadAssetAsync<T>(assetName);
        }
        private static async Task<T> LoadAssetAsync<T>(EAssetName asset)
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(asset.ToString());
            assetHandlers.Add((int)asset, handle);
            return await handle.Task;
        }

        // Spawn Unit => 얘는 Field, Battle 쪽으로 넘겨서 처리하는게 좋을 듯
        //public static async Task<T> SpawnUnit<T>(int index, Transform parent) where T : UnitBase, new()
        //{
        //    string code = GetAssetAddress(EAssetType.Prefab, (int)EPrefabType.UnitBase);
        //    GameObject obj = await InstantiateGameObjectAsync(code, parent, true);

        //    T unit = new();
        //    unit.Awake(index, obj.transform);

        //    string address = GetAssetAddress(EAssetType.AnimCtrl, index);
        //    UnityEngine.Assertions.Assert.IsNotNull(address, "Can`t Find Asset Address: " + address);
        //    Task<RuntimeAnimatorController> taskController = LoadAssetAsync<RuntimeAnimatorController>(address);
        //    await taskController;

        //    UnityEngine.Assertions.Assert.IsNotNull(taskController.Result, "Can`t Find Asset Data: " + address);
        //    unit.SetAnimeController(taskController.Result);

        //    taskController.Dispose();
        //    return unit;
        //}


        // Release Asset
        //public static void ClearAll()
        //{
        //    foreach (AsyncOperationHandle handler in objectHandlers.Values)
        //    {
        //        GameObject.Destroy(handler.Result as GameObject);
        //        Addressables.Release(handler);
        //    }
        //    objectHandlers.Clear();

        //    foreach (string code in assetHandlers.Keys)
        //    {
        //        Addressables.Release(assetHandlers[code]);
        //    }
        //    assetHandlers.Clear();
        //}
        //public static bool ReleaseGameObject(int instanceID)
        //{
        //    Addressables.Release(objectHandlers[instanceID]);
        //    return objectHandlers.Remove(instanceID);
        //}
        //public static bool ReleaseAsset(string code)
        //{
        //    Addressables.Release(assetHandlers[code]);
        //    return assetHandlers.Remove(code);
        //}
    }
}
