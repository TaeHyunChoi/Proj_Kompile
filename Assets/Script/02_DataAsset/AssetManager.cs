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

    public static async Task<T> CreateUIAsync<T>(string address, Transform parent, bool isOn) where T: x_UIBase, new()
    {
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent);
        GameObject go = await handle.Task;
        go.SetActive(isOn);

        T ui = new T();
        ui.Init(go);

        Handlers.Add(go.GetInstanceID(), handle);
        return ui;
    }

    public static bool ReleaseAsset(int instanceID)
    {
        Addressables.Release<GameObject>(Handlers[instanceID]);
        return Handlers.Remove(instanceID);
    }

    //객체 생성할 때에 gameObject도 한 번에 걸 수 있으나, 오버헤드가 발생할 수도 있음
    //public static async Task<T> CreateUIAsync<T>(string address, Transform parent, bool isOn) where T : UIBase
    //{
    //    AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent);
    //    GameObject go = await handle.Task;
    //    go.SetActive(isOn);

    //    // 리플렉션을 사용하여 T의 인스턴스 생성
    //    T ui = (T)System.Activator.CreateInstance(typeof(T), new object[] { go });

    //    Handlers.Add(go.GetInstanceID(), handle);
    //    return ui;
    //}
}
