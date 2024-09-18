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
    public void EnterFieldScene(int fieldSceneCode)
    {
        IRoutineUpdater loadScene;
        string mapCode;
        switch (fieldSceneCode)
        {
            case 900: mapCode = "020_FieldTestScene"; break;
            default:
                return;
        }
        loadScene = new IELoadScene(mapCode);

        IRoutineUpdater curtainOn = new IELoadingCurtainOn(mLoadingCurtain);
        IRoutineUpdater curtainOff = new IELoadingCurtainOff(mLoadingCurtain);

        IRoutineUpdater clearUI = new IEClearUI(exceptGroupType: EUIGroup.Field);

        IERoutine enterFieldRoutine = new IERoutine(curtainOn,
                                                    clearUI,
                                                    // clear prefab
                                                    loadScene,
                                                    // set field unit      
                                                    // set field camera
                                                    // set field ui       
                                                    curtainOff);

        mMain.AddCoroutine(enterFieldRoutine);
    }
}
