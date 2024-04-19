using System.Threading.Tasks;
using UnityEngine;

public class UIMgr
{
    private UIBase[] cache;

    private Canvas canvasOverlay;
    private Canvas canvasCamera;

    public Canvas OverlayCanvas { get => canvasOverlay; }
    public Canvas CameraCanvas  { get => canvasCamera; }

    public async Task InitAsync(GameState state)
    {
        switch (state)
        {
            case GameState.Opening:
                cache = new UIBase[1];
                GameObject obj = await AssetManager.InstantiateAsync("UITitle", CameraCanvas.transform, false);
                UITitle title = obj.AddComponent<UITitle>();
                cache[(byte)UIType.Title] = title;
                break;
            case GameState.Field:
                Debug.Log("Need to dev: UI.InitAsync(Field)");
                break;
        }
    }
    public void Pop(UIType type, bool isOn)
    {
        cache[(byte)type].Pop(isOn);
    }
    public void Release()
    {
        if (null == cache)
        {
            return;
        }

        for (int i = 0; i < cache.Length; ++i)
        {
            cache[i].Dispose();
        }
        cache = null;
    }

    public UIMgr(Transform transform)
    {
        canvasOverlay = transform.GetChild(0).GetComponent<Canvas>();
        canvasCamera = transform.GetChild(1).GetComponent<Canvas>();
    }
}
