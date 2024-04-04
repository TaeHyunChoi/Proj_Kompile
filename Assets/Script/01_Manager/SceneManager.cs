using UnityEngine;

public partial class SceneManager
{
    private CanvasGroup loadingCurtain;
    private SceneState state;
    public SceneState State { get => state; }

    public void LoadSceneAsync(ContentType type, int code)
    {
        MapData map = DataTable.MapTable.Find(x => x.Code == code);
        LoadLevel level = new LoadLevel(loadingCurtain, type, map);
        CoroutineUpdater.Get.SetHandler(new CCoroutine<LoadLevel>(level));
    }

    public void SetState(SceneState state)
    {
        this.state = state;
    }

    public SceneManager()
    {
        loadingCurtain = Main.UIMgr.GetOverlayCanvas().transform.GetChild(0).GetComponent<CanvasGroup>(); ;
        loadingCurtain.alpha = 0;
        loadingCurtain.gameObject.SetActive(false);
    }
}
