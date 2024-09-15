using UnityEngine;
using IECoroutine;

public class MapSceneManager_
{
    private CanvasGroup mLoadingCurtain;
    private Main_ mMain { get => Main_.Instance; }

    public MapSceneManager_(Transform transform)
    {
        Transform canvasTransform = transform.Find("CanvasCurtain");
        mLoadingCurtain = canvasTransform.GetComponentInChildren<CanvasGroup>();
    }

    public void LoadScene_Opening()
    {
        var opening = new IEOpeningScene(mLoadingCurtain);
        mMain.AddCoroutine(new CCoroutine<IEOpeningScene>(opening));
    }
}
