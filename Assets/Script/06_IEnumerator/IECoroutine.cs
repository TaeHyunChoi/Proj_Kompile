using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityEngine;
using System;

namespace IECoroutine
{
    public class IELoadOpeningScene : IRoutineUpdater
    {
        private AsyncOperation mLoadAsyncOper;
        private CanvasGroup mCurtainCanvas;

        private Task<GameObject> mTaskGetOpening;
        private Task<GameObject> mTaskGetUITitle;


        private Main_ Main { get => Main_.Instance; }

        public int MoveNext(int index)
        {
            switch (index)
            {
                case 0:
                    mCurtainCanvas.gameObject.SetActive(true);
                    break;
                case 1:
                    mLoadAsyncOper = SceneManager.LoadSceneAsync("010_OpeningScene", LoadSceneMode.Single);
                    break;
                case 2:
                    if (false == mLoadAsyncOper.isDone)
                    {
                        return index;
                    }
                    break;
                case 3:
                    Transform transformCameraCanvas = Main.RequestUI_GetCanvasCamera().transform;
                    Transform transformOverlayCanvas = Main.RequestUI_GetCanvasOverlay().transform;

                    mTaskGetOpening = Main.RequestAssetAysnc_GetPrefab(EAsset.OpeningGame, transformCameraCanvas);
                    mTaskGetUITitle = Main.RequestAssetAysnc_GetPrefab(EAsset.UITitle, transformOverlayCanvas);
                    break;
                case 4:
                    if (false == mTaskGetOpening.IsCompletedSuccessfully
                        || false == mTaskGetUITitle.IsCompletedSuccessfully)
                    {
                        return index;
                    }

                    //wait..
                    //Opening 코루틴 여러 개 붙인걸 만들어서 매개변수로 opening_prefab, ui_title_prefab 넘기면 될 듯?
                    //IEPlayOpening() 뭐 이런건가...
                    //OnOpening opening = new OnOpening(mTaskGetOpening.Result.transform);

                    break;
                default:
                    mLoadAsyncOper = null;
                    mCurtainCanvas = null;
                    mTaskGetOpening.Dispose();
                    mTaskGetUITitle.Dispose();
                    GC.Collect(0, GCCollectionMode.Forced);
                    return -1;
            }

            return index + 1;
        }

        public IELoadOpeningScene(CanvasGroup curtain)
        {
            mCurtainCanvas = curtain;
        }
    }
}
