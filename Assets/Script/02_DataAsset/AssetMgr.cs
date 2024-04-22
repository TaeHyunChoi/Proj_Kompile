using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

using System.Threading.Tasks;

public class AssetMgr
{
    private static Dictionary<int, AsyncOperationHandle> objectHandlers = new Dictionary<int, AsyncOperationHandle>();
    private static Dictionary<string, AsyncOperationHandle> assetHandler = new Dictionary<string, AsyncOperationHandle>();


    public static async Task<GameObject> InstantiateGameObjectAsync(string address, Transform parent, bool isOn)
    {
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent);
        GameObject go = await handle.Task;
        go.SetActive(isOn);

        objectHandlers.Add(go.GetInstanceID(), handle);
        return go;
    }

    public static Task<T> LoadAssetAsync<T>(string address)
    {
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
        assetHandler.Add(address, handle);
        return handle.Task;
    }

    public static Task<IList<T>> LoadAssetsInGroupAsync<T>(string groupCode)
    {
        AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(groupCode, null);
        assetHandler.Add(groupCode, handle);
        return handle.Task;
    }


    public static async Task<T> SpawnUnit<T>(int index, Transform parent) where T : UnitBase, new()
    {
        GameObject obj = await InstantiateGameObjectAsync("UnitBase", parent, true);

        T unit = new();
        unit.Awake(index, obj.transform);

        string groupCode = GetAnimeGroupCode(index);
        Task<IList<AnimationClip>> taskAnimeClips = LoadAssetsInGroupAsync<AnimationClip>(groupCode);
        await taskAnimeClips;
        UnityEngine.Assertions.Assert.IsNotNull(taskAnimeClips.Result, "Null Anime Clip: " + groupCode);

        AnimationClip[] clips = new List<AnimationClip>(taskAnimeClips.Result).ToArray();
        unit.SetAnimeClips(clips);

        taskAnimeClips.Dispose();
        return unit;
    }

    public static string GetAnimeGroupCode(int index)
    {
        switch (index)
        {
            case 0: return "Anime_Ataho";

        }
        return null;
    }

    public static bool ReleaseGameObject(int instanceID)
    {
        Addressables.Release(objectHandlers[instanceID]);
        return objectHandlers.Remove(instanceID);
    }

    //TODO: 확인 필요 - IList<T>도 일괄 해제되는가?
    public static bool ReleaseGroupAsset(string groupCode)
    {
        Addressables.Release(assetHandler[groupCode]);
        return assetHandler.Remove(groupCode);
    }
}
