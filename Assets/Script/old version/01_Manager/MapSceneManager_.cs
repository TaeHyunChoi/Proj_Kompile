using UnityEngine;
using IECoroutine;
using System.Collections.Generic;

public class MapSceneManager_
{
    private CanvasGroup mLoadingCurtain;
    private List<int> mInstanceIDCache;

    //private x_Main_ mMain { get => x_Main_.Instance; }

    public MapSceneManager_(Transform transform)
    {
        Transform canvasTransform = transform.Find("CanvasCurtain");
        mLoadingCurtain = canvasTransform.GetComponentInChildren<CanvasGroup>();
        mInstanceIDCache = new List<int>();
    }

    public void StartGame()
    {
        IRoutineUpdater loadScene  = new IELoadScene("010_OpeningScene");
        IRoutineUpdater enterScene = new IEOpeningScene(mLoadingCurtain);

        IERoutine routine = new IERoutine(loadScene, enterScene);
        //mMain.AddCoroutine(routine);
    }
    public void EnterFieldScene(int fieldSceneCode)
    {
        string mapCode;
        switch (fieldSceneCode)
        {
            case 900: mapCode = "020_FieldTestScene"; break;
            default:  return;
        }

        var curtainOn     = new IELoadingCurtainOn(mLoadingCurtain);
        //var clearUI       = new IEClearUI(exceptGroupType: EUIGroup.Field);
        var clearMapScene = new IEClearMapSceneObjects();
        var loadScene     = new IELoadScene(mapCode);
        var curtainOff    = new IELoadingCurtainOff(mLoadingCurtain);

        IERoutine enterFieldRoutine = new IERoutine(curtainOn,
                                                    //clearUI,
                                                    clearMapScene,
                                                    loadScene,
                                                    // set field unit (playable, npc, ...)
                                                    // set field camera
                                                    // set field ui       
                                                    curtainOff);

        //mMain.AddCoroutine(enterFieldRoutine);
    }

    public void AddNewObject(int instanceID) => mInstanceIDCache.Add(instanceID);
    public void ClearObjects()
    {
        for (int i = 0; i < mInstanceIDCache.Count; ++i)
        {
            //AssetManager.ReleaseGameObject(mInstanceIDCache[i]);
        }

        mInstanceIDCache.Clear();
    }
}
