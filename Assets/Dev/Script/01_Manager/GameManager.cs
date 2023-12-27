using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public class GameManager : MonoBehaviour
{
    public void InitContent(ContentType type)
    {
        IEnumerator coroutine;
        switch (type)
        {
            case ContentType.Opening: coroutine = IETitleAsync(); break;
            default: /* Do Nothing. */  return;
        }

        StartCoroutine(coroutine);
    }
    private IEnumerator IETitleAsync()
    {
        UIManager uiMgr = Main.GetUIManager();
        Transform transfromCameraCanvas = uiMgr.GetCameraCanvas().transform;

        Task<OnOpening> taskOpening = OnOpening.InitAsync(transfromCameraCanvas);
        Task<GameObject> taskUITitle = AssetManager.InstantiateAsync("UITitle", transfromCameraCanvas, false);

        yield return new WaitUntil(() => taskOpening.IsCompleted);
        yield return new WaitUntil(() => taskUITitle.IsCompleted);

        taskOpening.Result.Init();
        uiMgr.SetUI(UIType.Title, taskUITitle.Result.AddComponent<UITitle>());

        taskOpening.Dispose();
        taskUITitle.Dispose();
    }
}
