using UnityEngine;

public class UIManager_
{
    public Canvas CanvasCamera { get; private set; }
    public Canvas CanvasOverlay { get; private set; }

    private UIComponent[] mUICache;

    public UIManager_(Transform transfrom)
    {
        CanvasCamera  = transfrom.Find("CanvasCamera").GetComponent<Canvas>();
        CanvasOverlay = transfrom.Find("CanvasOverlay").GetComponent<Canvas>();
        mUICache = new UIComponent[(EUIType.Count).ToInt()];
    }

    public void AddNew(EUIType type, GameObject obj) => mUICache[type.ToInt()] = new UIComponent(type, obj);
    public void Open(EUIType type) => mUICache[type.ToInt()].Open();
    public void Close(EUIType type) => mUICache[type.ToInt()].Close();
    public void Clear(EUIGroup exceptGroupType)
    {
        for (int i = 0; i < mUICache.Length; ++i)
        {
            if (exceptGroupType != mUICache[i].UIGroup)
            {
                mUICache[i].Close();
                AssetMgr.ReleaseGameObject(mUICache[i].InstanceID);
            }
        }
    }
}
