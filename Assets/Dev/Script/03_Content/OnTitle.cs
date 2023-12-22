using UnityEngine;
using System.Threading.Tasks;
using System;

internal class OnTitle : MonoBehaviour
{
    private static OnTitle instance;

    public static async Task<OnTitle> InitAsync(Transform canvas_ui)
    {
        try
        {
            GameObject obj = await AssetManager.InstantiateAsync("UITitle", canvas_ui);
            instance = obj.AddComponent<OnTitle>();
            obj.SetActive(false);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error loading assets: " + ex.Message);
            return null;
        }

        return instance;
    }
    private void OnDestroy()
    {
        AssetManager.ReleaseAsset(gameObject.GetInstanceID());
    }
}
