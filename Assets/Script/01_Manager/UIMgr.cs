using System.Threading.Tasks;
using UnityEngine;

public class UIMgr
{
    private UIBase[] mUICache;
    public Canvas CanvasOverlay { get; private set; }
    public Canvas CanvasCamera  { get; private set; }

    public async Task InitAsync(EGameStateFlag state)
    {
        switch (state)
        {
            case EGameStateFlag.Opening:
                string code = AssetMgr.GetAssetAddress(EAssetType.UI, (int)EUIType.Title);
                GameObject obj = await AssetMgr.InstantiateGameObjectAsync(code, CanvasCamera.transform, false);
                UITitle title = obj.AddComponent<UITitle>();
                mUICache[(byte)EUIType.Title] = title;
                break;
            case EGameStateFlag.Field:
                Debug.Log("Need to dev: UI.InitAsync(Field)");
                break;
        }
    }
    public void Pop(EUIType type, bool isOn)
    {
        Main.InputMgr.Updater = null;
        mUICache[(byte)type].Pop(isOn);
    }
    public void Release()
    {
        UnityEngine.Assertions.Assert.IsNotNull(mUICache, "null ui cache");

        for (int i = 0; i < mUICache.Length; ++i)
        {
            if (null == mUICache[i])
            {
                break;
            }

            mUICache[i].Release();
        }
    }

    public UIMgr(Transform transform)
    {
        mUICache = new UIBase[8]; //임의로 설정
        CanvasOverlay = transform.GetChild(0).GetComponent<Canvas>();
        CanvasCamera  = transform.GetChild(1).GetComponent<Canvas>();
    }
}
