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
    //public static async Task<GameObject> InstantiateAsync(string code, Transform canvas_tf, bool isActive = false)
    //{
    //    AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(code);

    //    handle.Completed += (handle) =>
    //    {
    //        GameObject prefab = Object.Instantiate(handle.Result, canvas_tf);
    //        Handlers.Add(prefab.GetInstanceID(), handle);
    //        prefab.SetActive(isActive);
    //    };
    //    await handle.Task;

    //    if (handle.Status != AsyncOperationStatus.Succeeded)
    //    {
    //        Debug.LogError($"Failed to load asset with key: {code}");
    //        return null;
    //    }

    //    return handle.Result;
    //}

    public static bool ReleaseAsset(int instanceID)
    {
        Addressables.Release<GameObject>(Handlers[instanceID]);
        return Handlers.Remove(instanceID);
    }

    /*
    public static async Task InstantiateUI(ContentType type, Transform parent)
    {
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        //이전 UI 삭제
        foreach (var ui in Handlers.Values)
        {
            Addressables.Release(ui);
            Object.Destroy(ui.Result);
        }
        Handlers.Clear();

        //호출 UI 생성
        switch (type)
        {
            case ContentType.Title:
                {
                    task.Add(InstantiateAsync<UIBattle>("UIBattle", parent));
                    task.Add(InstantiateAsync<UIBattleSlot>("UIBattle_MenuSlot", parent));
                }
                break;
        }
        await Task.WhenAll(task);
        task.Clear();

        watch.Stop();
        Debug.Log("AssetManager.InstantiateUI: " + watch.ElapsedMilliseconds + "ms");
    }
    //*/
}
