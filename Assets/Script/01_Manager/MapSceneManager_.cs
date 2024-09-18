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

    public void StartGame()
    {
        IRoutineUpdater loadScene  = new IELoadScene("010_OpeningScene");
        IRoutineUpdater enterScene = new IEOpeningScene(mLoadingCurtain);

        IERoutine routine = new IERoutine(loadScene, enterScene);
        mMain.AddCoroutine(routine);
    }
}
