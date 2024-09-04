using UnityEngine;
using DataStruct;

public partial class SceneMgr
{
    private CanvasGroup mCurtainCanvas;

    public void LoadSceneAsync(EGameStateFlag next, int code = -1)
    {
        Main.InputMgr.Updater      = null;
        Main.InputMgr.FixedUpdater = null;

        switch (next)
        {
            case EGameStateFlag.Opening:
                LoadOpeningScene opening = new LoadOpeningScene(mCurtainCanvas);
                CoroutineUpdater.SetHandler(new CCoroutine<LoadOpeningScene>(opening));
                break;
            case EGameStateFlag.Field:
                if (true == DataTable.TryGetMapData(code, out MapData map))
                {
                    LoadFieldScene level = new LoadFieldScene(mCurtainCanvas, map);
                    CoroutineUpdater.SetHandler(new CCoroutine<LoadFieldScene>(level));
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
