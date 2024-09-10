using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IELoadSceneOpening : IRoutineUpdater
{
    private AsyncOperation mLoadAsyncOper;
    private CanvasGroup mCurtainCanvas;

    private Task<OnOpening> mTaskOpening;
    private Task mTaskLoadUI;

    private Main_ main { get => Main_.Instance; }

    public int MoveNext(int index)
    {
        switch (index)
        {
            case 0:
                string sceneName = String_.GetSceneName(EScene.Opening);
                mLoadAsyncOper = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                break;
            case 1:
                if (false == mLoadAsyncOper.isDone)
                {
                    return index;
                }
                break;
            case 2:
                Canvas cameraCanvas = main.RequestUI_GetCanvasCamera();

                //Asset도 건드려야 하네 => 이걸 좀 더 적은 메소드로 처리할 순 없나?
                //mTaskOpening = OnOpening.InitAsync(cameraCanvas.transform);
                //mTaskLoadUI = Main.UIMgr.InitAsync(EGameStateFlag.Opening);
                break;
            case 3:
                if (false == mTaskOpening.IsCompletedSuccessfully
                    || false == mTaskLoadUI.IsCompletedSuccessfully)
                {
                    return index;
                }

                mTaskOpening.Result.Set();
                mCurtainCanvas.gameObject.SetActive(false);
                break;
            default:
                mTaskOpening.Dispose();
                mTaskLoadUI.Dispose();
                return -1;
        }

        return index + 1;
    }

    public IELoadSceneOpening(CanvasGroup curtain)
    {
        mCurtainCanvas = curtain;
        mCurtainCanvas.gameObject.SetActive(true);
    }
}
