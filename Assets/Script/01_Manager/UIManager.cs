using System;
using System.Collections;
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
        canvas_camera = transform.GetChild(1).GetComponent<Canvas>();
        uiBucket = new UIBase[(int)UIType.Count];
        currentType = UIType.None;
    }

    public void Set(ContentType type)
    {
        Coroutiner.PlayCoroutine(IEInitUIAsync(type));
    }

    private IEnumerator IEInitUIAsync(ContentType contentType)
    {
        switch (contentType)
        {
            case ContentType.Opening:
                {
                    Task<UITitle> task_Title = AssetManager.CreateUIAsync<UITitle>("UITitle", canvas_camera.transform, false);
                    yield return new WaitUntil(() => task_Title.IsCompletedSuccessfully);
                    uiBucket[(int)UIType.Title] = task_Title.Result;
                    task_Title.Dispose();
                }
                break;
            case ContentType.Field:
                {

                }
                break;
        }
    }
    public void OpenUI(UIType type)
    {
        currentType = type;
        this.uiBucket[(int)currentType].Open();
        Main.InputMgr.Set(uiBucket[(int)currentType].Input);
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

    public void SetBucket(int index, UIBase ui)
    {
        uiBucket[index] = ui;
    }
    public void Dispose(ContentType type)
    { 
        switch(type)
        {
            case ContentType.Opening:
                uiBucket[(int)UIType.Title].Close();
                uiBucket[(int)UIType.Title] = null;
                break;
        }

        currentType = UIType.None;
    }
}
