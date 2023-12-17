using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

using System.Threading.Tasks;

public class AssetManager
{
    private static Dictionary<int, AsyncOperationHandle<GameObject>> Handlers = new Dictionary<int, AsyncOperationHandle<GameObject>>();

    public static async Task<GameObject> InstantiateAsync(string address, Transform parent = null)
    {
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent);
        await handle.Task;
        Handlers.Add(handle.Result.GetInstanceID(), handle);

        return handle.Result;
    }
    public static bool ReleaseAsset(int instanceID)
    {
        Addressables.Release<GameObject>(Handlers[instanceID]);
        return Handlers.Remove(instanceID);
    }
}
