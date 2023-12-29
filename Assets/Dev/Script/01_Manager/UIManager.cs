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
        currentUI = UIType.None;
    }

    public async Task<T> InitAsync<T>(UIType type, Transform parent, bool isActive) where T : UIBase, new()
    {
        string address = string.Empty;
        switch (type)
        {
            case UIType.Title:      address = "UITitle";        break;
        }

        try
        {
            GameObject go = await AssetManager.InstantiateAsync(address, parent, false);
            this.uiBucket[(int)type] = new T() as UIBase;
            this.uiBucket[(int)type].Init(go);
            go.SetActive(isActive);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading assets: UIType.{type} ({ex.Message})");
            return null;
        }

        return this.uiBucket[(int)type] as T;
    }

    public void OpenUI(UIType type)
    {
        this.uiBucket[(int)type].Open();
        currentUI = type;
        Main.InputMgr.SetInputDele(uiBucket[(int)type].Input);
    }
    public void SetCurrent(UIType type, out InputDele func)
    {
        currentUI = type;
        func = uiBucket[(int)type].Input;
    }

    public void Update()
    {
        if (currentUI != UIType.None) //지금 이게 마음에 안 든다는거 아녀?
        {
            uiBucket[(int)currentUI].Update();
        }
    }
    public void CloseCurrentUI()
    {
        uiBucket[(int)currentUI].Close();
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