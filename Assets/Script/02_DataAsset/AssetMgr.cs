using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

using System.Threading.Tasks;

public class AssetMgr
{
    private static Dictionary<int,    AsyncOperationHandle> objectHandlers = new Dictionary<int,    AsyncOperationHandle>();
    private static Dictionary<string, AsyncOperationHandle> assetHandler   = new Dictionary<string, AsyncOperationHandle>();

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
        assetHandler.Add(address, handle);
        return handle.Task;
    }

    // Spawn Unit
    public static async Task<T> SpawnUnit<T>(int index, Transform parent) where T : UnitBase, new()
    {
        GameObject obj = await InstantiateGameObjectAsync("UnitBase", parent, true);

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
    public static string GetAssetAddress(EAssetType type, int code)
    {
        int index = (byte)type * 10000 + code;

        switch (index)
        {
            // Unit
            case 01_0000: return "AnimCtrl_Ataho";
            case 01_0001: return "AnimCtrl_Linxhang";
            case 01_0002: return "AnimeCtrl_Smashu";

                // UI
                //...

                // Sound
                // ...
        }

        return null;
    }

    // Release Asset
    public static bool ReleaseGameObject(int instanceID)
    {
        Addressables.Release(objectHandlers[instanceID]);
        return objectHandlers.Remove(instanceID);
    }
    public static bool ReleaseAsset(string code)
    {
        Addressables.Release(assetHandler[code]);
        return assetHandler.Remove(code);
    }
}
