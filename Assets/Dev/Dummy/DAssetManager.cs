using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DAssetManager
{
    private static bool isTest = true;
    private static AsyncOperationHandle handle;
    private static GameObject prefab;
    public static bool TestLoad()
    {
        if (isTest)
        {
            Addressables.LoadAssetAsync<GameObject>("Prefab/UnitBase").Completed
                += ((AsyncOperationHandle<GameObject> obj) => 
                { 
                    handle = obj;
                    prefab = GameObject.Instantiate(obj.Result);
                });
        }
        else
        {
            Addressables.Release(handle);
            GameObject.Destroy(prefab);
        }

        return isTest = !isTest;
    }
}
