using System;
using System.Threading.Tasks;
using UnityEngine;

public class UIManager
{
    private Canvas canvas_overlay;
    private Canvas canvas_camera;

    private UIBase[] uiBucket;
    private UIType currentUI;

    public UIManager(Transform transform)
    {
        canvas_overlay = transform.GetChild(0).GetComponent<Canvas>();
        canvas_camera  = transform.GetChild(1).GetComponent<Canvas>();
        uiBucket = new UIBase[(int)UIType.Count];
    }

    public async Task<T> InitAsync<T>(UIType type, Transform parent, bool isActive) where T : UIBase
    {
        T result;
        string address = string.Empty;
        switch (type)
        {
            case UIType.Title:      address = "UITitle";        break;
        }

        try
        {
            GameObject go = await AssetManager.InstantiateAsync(address, parent, false);
            this.uiBucket[(int)type] = result = go.AddComponent<T>();
            go.SetActive(isActive);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading assets: UIType.{type} ({ex.Message})");
            return null;
        }

        return result;
    }
    public void AddUIBucket(UIType type, UIBase ui)
    {
        this.uiBucket[(int)type] = ui;
    }


    public void OpenUI(UIType type)
    {
        this.uiBucket[(int)type].Open();
    }
    public void SetCurrentUI(UIType type, out InputDele func)
    {
        currentUI = type;
        func = uiBucket[(int)type].Input;
    }


    public Canvas GetOverlayCanvas()
    {
        return canvas_overlay;
    }
    public Canvas GetCameraCanvas()
    {
        return canvas_camera;
    }
}