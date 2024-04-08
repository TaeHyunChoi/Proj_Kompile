using UnityEngine;

public partial class SceneMgr
{
    private CanvasGroup loadingCurtain;
    private SceneState state;

    public void LoadSceneAsync(GameState next, int code = -1)
    {
        switch (next)
        {
            case GameState.Opening:
                LoadOpeningScene opening = new LoadOpeningScene(loadingCurtain);
                CoroutineUpdater.Get.SetHandler(new CCoroutine<LoadOpeningScene>(opening));
                break;
            case GameState.Field:
                if (true == DataTable.TryGetMapData(code, out MapData map))
                {
                    LoadFieldScene level = new LoadFieldScene(loadingCurtain, map);
                    CoroutineUpdater.Get.SetHandler(new CCoroutine<LoadFieldScene>(level));
                }
                else
                {
                    Debug.LogError("Wrong Field Map code: " + code);
                    return;
                }
                break;
        }
    }
    public void SetState(SceneState state)
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
