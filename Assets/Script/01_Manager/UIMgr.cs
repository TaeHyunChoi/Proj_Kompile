using System.Threading.Tasks;
using UnityEngine;

public class UIMgr
{
    private UIBase[] cache;

    private Canvas canvasOverlay;
    private Canvas canvasCamera;

    public Canvas OverlayCanvas { get => canvasOverlay; }
    public Canvas CameraCanvas  { get => canvasCamera; }

    public async Task InitUIAsync(GameState state)
    {
        switch (state)
        {
            case GameState.Opening:
                cache = new UIBase[1];
                UITitle title = await AssetManager.CreateUIAsync<UITitle>("UITitle", CameraCanvas.transform, false);
                cache[(byte)UIType.Title] = title;
                break;
        }
    }
    public void Pop(UIType type, bool isOn)
    {
        cache[(byte)type].Pop(isOn);
    }

    public UIMgr(Transform transform)
    {
        canvasOverlay = transform.GetChild(0).GetComponent<Canvas>();
        canvasCamera = transform.GetChild(1).GetComponent<Canvas>();
    }
}
