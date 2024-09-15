using UnityEngine;
using DataStruct;

public partial class SceneMgr
{
    private CanvasGroup mCurtainCanvas;

    private Main_ mMain { get => Main_.Instance; }

    public void LoadSceneAsync(EGameStateFlag next, int code = -1)
    {
        switch (next)
        {
            case EGameStateFlag.Opening:
                //LoadOpeningScene opening = new LoadOpeningScene(mCurtainCanvas);
                //CoroutineUpdater.SetHandler(new CCoroutine<LoadOpeningScene>(opening));
                break;
            case EGameStateFlag.Field:
                if (true == DataTable.TryGetMapData(code, out MapData map))
                {
                    LoadFieldScene level = new LoadFieldScene(mCurtainCanvas, map);
                    mMain.AddCoroutine(new CCoroutine<LoadFieldScene>(level));
                }
                else
                {
                    Debug.LogError("Wrong Field Map code: " + code);
                    return;
                }
                break;
        }
    }

    public SceneMgr(Transform transform)
    {
        transform = transform.GetChild(0);
        mCurtainCanvas = transform.GetComponent<CanvasGroup>();
        mCurtainCanvas.gameObject.SetActive(false);
    }
}
