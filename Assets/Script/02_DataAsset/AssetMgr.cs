using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

using System.Threading.Tasks;

public class AssetMgr
{
    private static Dictionary<int, AsyncOperationHandle<GameObject>> Handlers = new Dictionary<int, AsyncOperationHandle<GameObject>>();
    private static Dictionary<string, AsyncOperationHandle> Assets = new Dictionary<string, AsyncOperationHandle>();

    public static async Task<GameObject> InstantiateGameObjectAsync(string address, Transform parent, bool isOn)
    {
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent);
        GameObject go = await handle.Task;
        go.SetActive(isOn);

        Handlers.Add(go.GetInstanceID(), handle);
        return go;
    }
    public static async Task<T> LoadAssetAsync<T>(string address)
    {
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
        T asset = await handle.Task;
        Assets.Add(address, handle);
        return asset;
    }


    public static async Task<T> SpawnUnit<T>(int index, Transform parent) where T : UnitBase, new()
    {
        GameObject obj = await InstantiateGameObjectAsync("UnitBase", parent, true);
        T unit = new T();
        await unit.AwakeAsync(index, obj.transform);

        return unit;
    }

    public static bool ReleaseAsset(int instanceID)
    {
        Addressables.Release<GameObject>(Handlers[instanceID]);
        return Handlers.Remove(instanceID);
    }
    public static bool ReleaseAsset(string[] address)
    {
        string key;
        for (int i = 0; i < address.Length; ++i)
        {
            key = address[i];
            Addressables.Release(Assets[key]);

            if (false == Assets.Remove(key))
            {
                return false;
            }
        }

        return true;
    }
}
