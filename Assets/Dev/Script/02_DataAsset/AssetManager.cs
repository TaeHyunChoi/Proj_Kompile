using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

using System.Threading.Tasks;

public class AssetManager
{
    private static Dictionary<int, AsyncOperationHandle<GameObject>> Handlers = new Dictionary<int, AsyncOperationHandle<GameObject>>();

    public static async Task<GameObject> InstantiateAsync(string address, Transform parent, bool isActive)
    {
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent);

        GameObject go = await handle.Task;
        go.SetActive(isActive);
        Handlers.Add(go.GetInstanceID(), handle);

        return go;
    }
    public static bool ReleaseAsset(int instanceID)
    {
        Addressables.Release<GameObject>(Handlers[instanceID]);
        return Handlers.Remove(instanceID);
    }
}
