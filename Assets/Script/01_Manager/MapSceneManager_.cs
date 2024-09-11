using UnityEngine;
using IECoroutine;

public class MapSceneManager_
{
    private CanvasGroup mLoadingCurtain;
    public MapSceneManager_(Transform transform)
    {
        Transform canvasTransform = transform.Find("CanvasCurtain");
        mLoadingCurtain = canvasTransform.GetComponentInChildren<CanvasGroup>();
    }

    public void LoadScene_Opening()
    {
        var opening = new IEOpeningScene(mLoadingCurtain);
        Main_.Instance.AddCoroutine(new CCoroutine<IEOpeningScene>(opening));
    }
}
