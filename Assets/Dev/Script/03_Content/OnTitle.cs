using UnityEngine;
using System.Threading.Tasks;
using System;

internal class OnTitle : MonoBehaviour
{
    //코드 중복?
    public static async Task<bool> InitAsync(Transform canvas_ui)
    {
        try
        {
            GameObject obj = await AssetManager.InstantiateAsync("UITitle", canvas_ui);
            obj.AddComponent<OnTitle>();
            obj.SetActive(false);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error loading assets: " + ex.Message);
            return false;
        }

        return true;
    }
    private int current = 0;

    private void Awake()
    {
        
    }
    private void Start()
    {

    }
    private void Update()
    {
        
    }
    private void OnDestroy()
    {
        AssetManager.ReleaseAsset(gameObject.GetInstanceID());
    }

    public void MoveNext()
    {

    }
}
