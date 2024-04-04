using UnityEngine;

public partial class SceneManager
{
    private CanvasGroup loadingCurtain;
    private SceneState state;
    public SceneState State { get => state; }

    public void LoadSceneAsync(int code)
    {
        MapData map = DataTable.MapTable.Find(x => x.Code == code);
        LoadScene level = new LoadScene(loadingCurtain, map);
        CoroutineUpdater.Get.SetHandler(new CCoroutine<LoadScene>(level));
    }
    public void SetState(SceneState state)
    {
        this.state = state;
    }

    public SceneManager()
    {
        loadingCurtain = Main.UIMgr.GetOverlayCanvas().transform.GetChild(0).GetComponent<CanvasGroup>();
        loadingCurtain.gameObject.SetActive(false);
    }
}
