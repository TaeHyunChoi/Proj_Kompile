using UnityEngine;

public class UIManager
{
    private Canvas canvas_overlay;
    private Canvas canvas_camera;

    //얘도 코드 직관성이 떨어지네. 수정합시다.
    private UIBase[] uiBucket;
    private UIType currentType;

    public UIManager(Transform transform)
    {
        canvas_overlay = transform.GetChild(0).GetComponent<Canvas>();
        canvas_camera = transform.GetChild(1).GetComponent<Canvas>();
        uiBucket = new UIBase[(int)UIType.Count];
        currentType = UIType.None;
    }

    public void OpenUI(UIType type)
    {
        currentType = type;
        this.uiBucket[(int)currentType].Open();
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

    public void SetBucket(UIType type, UIBase ui)
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
