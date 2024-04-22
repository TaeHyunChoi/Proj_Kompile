using UnityEngine;

public partial class SceneMgr
{
    private CanvasGroup loadingCurtain;
    private ESceneState state;

    public void LoadSceneAsync(EGameStateFlag next, int code = -1)
    {
        Main.InputMgr.ReleaseUpdater();
        Main.InputMgr.ReleaseFixedUpdater();

        switch (next)
        {
            case EGameStateFlag.Opening:
                LoadOpeningScene opening = new LoadOpeningScene(loadingCurtain);
                CoroutineUpdater.SetHandler(new CCoroutine<LoadOpeningScene>(opening));
                break;
            case EGameStateFlag.Field:
                if (true == DataTable.TryGetMapData(code, out MapData map))
                {
                    LoadFieldScene level = new LoadFieldScene(loadingCurtain, map);
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
    public void SetState(ESceneState state)
    {
        this.state = state;
    }

    public SceneMgr(Transform transform)
    {
        transform = transform.GetChild(0);
        loadingCurtain = transform.GetComponent<CanvasGroup>();
        loadingCurtain.gameObject.SetActive(false);
    }
}
