using System;
using System.Threading.Tasks;
using UnityEngine;

public class UIManager
{
    private Canvas canvas_overlay;
    private Canvas canvas_camera;

    private UIBase[] uiBucket;
    private UIType currentType;

    public UIManager(Transform transform)
    {
        canvas_overlay = transform.GetChild(0).GetComponent<Canvas>();
        canvas_camera  = transform.GetChild(1).GetComponent<Canvas>();
        uiBucket = new UIBase[2];
        currentType = UIType.None;
    }

    public async Task<T> InitAsync<T>(UIType type, Transform parent, bool isActive) where T : UIBase, new()
    {
        string address = string.Empty;
        T ui;
        switch (type)
        {
            case UIType.Title:      address = "UITitle";        break;
            case UIType.SaveData:   address = "UISaveData";     break;
        }

        try
        {
            GameObject go = await AssetManager.InstantiateAsync(address, parent, false);
            ui = new T();
            this.uiBucket[(int)type] = ui;
            ui.Init(go);
            go.SetActive(isActive);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading assets: UIType.{type} ({ex.Message})");
            return null;
        }

        return ui;
    }

    public void OpenUI(UIType type)
    {
        currentType = type;
        this.uiBucket[(int)currentType].Open();
        Main.InputMgr.SetInputDele(uiBucket[(int)currentType].Input);
    }

    public void Update()
    {
        if (currentType != UIType.None)
        {
            uiBucket[(int)currentType].Update();
        }
    }
    public void CloseCurrentUI()
    {
        uiBucket[(int)currentType].Close();
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