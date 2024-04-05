using UnityEngine;

public class x_UIMgr
{
    private Canvas canvas_overlay;
    private Canvas canvas_camera;

    private x_UIBase[] uiBucket;
    private UIType currentType;

    public x_UIMgr(Transform transform)
    {
        canvas_overlay = transform.GetChild(0).GetComponent<Canvas>();
        canvas_camera = transform.GetChild(1).GetComponent<Canvas>();
        uiBucket = new x_UIBase[(int)UIType.Count];
        currentType = UIType.None;
    }

    public void OpenUI(UIType type)
    {
        currentType = type;
        uiBucket[(int)currentType].Open();
    }
    public void Update()
    {
        if (currentType != UIType.None)
        {
            uiBucket[(int)currentType].Update();
        }
    }
    public Canvas GetOverlayCanvas()
    {
        return canvas_overlay;
    }
    public Canvas GetCameraCanvas()
    {
        return canvas_camera;
    }

    public void Set(UIType type, x_UIBase ui)
    {
        uiBucket[(byte)type] = ui;
    }
    public void Dispose(GameState state)
    { 
        switch(state)
        {
            case GameState.Opening:
                uiBucket[(int)UIType.Title].Close();
                uiBucket[(int)UIType.Title] = null;
                break;
        }

        currentType = UIType.None;
    }
}
