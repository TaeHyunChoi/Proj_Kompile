using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEditor;
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
        IEnumerator coroutine;
        switch (type)
        {
            case ContentType.Opening:
                coroutine = IEInitUIAsync<UITitle>((int)UIType.Title, "UITitle", canvas_camera.transform, true);
                Coroutiner.PlayCoroutine(coroutine);
                break;
            case ContentType.Field:
                //이거 맞냐..? 흠..
                //coroutine = IEInitUIAsync<UIHUD>();
                //Coroutiner.PlayCoroutine(coroutine);

                //coroutine = IEInitUIAsync<UIInteraction>();
                //Coroutiner.PlayCoroutine(coroutine);

                //coroutine = IEInitUIAsync<UITrade>();
                //Coroutiner.PlayCoroutine(coroutine);

                //coroutine = IEInitUIAsync<UIDialogue>();
                //Coroutiner.PlayCoroutine(coroutine);

                break;
            default: /* Do Nothing. */ return;
        }
    }

    //여기서 제네릭<T>를 사용하지 않으면... 흠...
    private IEnumerator IEInitUIAsync<T>(int typeIndex, string address, Transform parent, bool isOn) where T : UIBase, new()
    {
        Task<GameObject> task = AssetManager.InstantiateAsync(address, parent, false);
        yield return new WaitUntil(() => task.IsCompletedSuccessfully);

        GameObject go = task.Result;
        T ui = new T();
        ui.Init(go);
        uiBucket[typeIndex] = ui;
        go.SetActive(isOn);

        task.Dispose();
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