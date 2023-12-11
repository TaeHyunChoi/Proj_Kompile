using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

using System.Threading.Tasks;

public class AssetManager
{
    private static Dictionary<int, AsyncOperationHandle<GameObject>> Handlers = new Dictionary<int, AsyncOperationHandle<GameObject>>();
    //private static List<Task> task = new List<Task>();

    public static async Task<GameObject> InstantiateAsync(string code, Transform canvas_tf)
    {
        var completionSource = new TaskCompletionSource<GameObject>();
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(code);

        handle.Completed += (handle) =>
        {
            GameObject prefab = Object.Instantiate(handle.Result, canvas_tf);
            Handlers.Add(prefab.GetInstanceID(), handle);
            prefab.SetActive(false);
        };

        await handle.Task;
        return await completionSource.Task;
    }
    public static async Task<GameObject> InstantiateAsync<T>(string code, Transform canvas_tf)  where T:MonoBehaviour
    {
        var completionSource = new TaskCompletionSource<GameObject>();
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(code);

        handle.Completed += (handle) =>
            {
                GameObject prefab = Object.Instantiate(handle.Result, canvas_tf);
                prefab.AddComponent<T>();
                Handlers.Add(prefab.GetInstanceID(), handle);
                prefab.SetActive(false);
            };

        await handle.Task;
        return await completionSource.Task;
    }

    public static GameObject GetInstance(int id)
    {
        return Handlers[id].Result;
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
