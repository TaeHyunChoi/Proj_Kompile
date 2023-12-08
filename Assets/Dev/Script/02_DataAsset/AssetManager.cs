using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

using System.Linq;
using System.Threading.Tasks;

public class AssetManager
{
    private static Dictionary<int, AsyncOperationHandle<GameObject>> handles = new Dictionary<int, AsyncOperationHandle<GameObject>>();

    public static void Instantiate(string code, Transform canvas_tf)
    {
        GameObject prefab = null;

        Addressables.LoadAssetAsync<GameObject>(code).Completed 
            += (AsyncOperationHandle<GameObject> handle) => 
            {
                prefab = Object.Instantiate(handle.Result, canvas_tf);
                prefab.SetActive(false);
                handles.Add(prefab.GetInstanceID(), handle);
            };
    }

    public static async Task Wait(/* 매개변수 opened를 넘긴다면? */)
    {
        await Task.WhenAll(handles.Values.Select(handle => handle.Task));

        string debug = string.Empty;
        foreach(var key in handles.Keys)
        {
            debug += (key + " / ");
        }

        Debug.Log(debug);
    }
    //public static bool TestLoad()
    //{
    //    if (isTest)
    //    {
    //        Addressables.LoadAssetAsync<GameObject>("Prefab/UnitBase").Completed
    //            += ((AsyncOperationHandle<GameObject> obj) => 
    //            { 
    //                handle = obj;
    //                prefab = GameObject.Instantiate(obj.Result);
    //            });
    //    }
    //    else
    //    {
    //        Addressables.Release(handle);
    //        GameObject.Destroy(prefab);
    //    }

    //    return isTest = !isTest;
    //}
}
