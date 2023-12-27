using System;
using System.Threading.Tasks;
using UnityEngine;

public class UIManager
{
    private Canvas canvas_overlay;
    private Canvas canvas_camera;

    private UIBase[] ui;

    public UIManager(Transform transform)
    {
        canvas_overlay = transform.GetChild(0).GetComponent<Canvas>();
        canvas_camera  = transform.GetChild(1).GetComponent<Canvas>();
        ui = new UIBase[(int)UIType.Count];
    }

    public void SetUI(UIType type, UIBase ui)
    {
        this.ui[(int)type] = ui;
    }
    public UIBase GetUI(UIType type)
    {
        return this.ui[(int)type];
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