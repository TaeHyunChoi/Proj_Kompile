using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static Index.IDxInput;

namespace IECoroutine
{
    #region Opening
    public class IEOpeningScene : IRoutineUpdater
    {
        private AsyncOperation mLoadAsyncOper;
        private CanvasGroup mCurtainCanvas;

        private Task<GameObject> mTaskGetOpening;
        private Task<GameObject> mTaskGetUITitle;

        private Transform mParentTransform;
        private CCoroutineHandler mCoroutineHandler;

        private Main_ Main { get => Main_.Instance; }

        public int MoveNext(int index)
        {
            switch (index)
            {
                /* load scene */
                case 0:
                    mLoadAsyncOper = SceneManager.LoadSceneAsync("010_OpeningScene", LoadSceneMode.Single);
                    break;
                case 1:
                    if (false == mLoadAsyncOper.isDone)
                    {
                        return index;
                    }
                    break;

                /* load prefabs */
                case 2:
                    Transform transformCameraCanvas = Main.RequestUI_GetCanvasCamera().transform;
                    Transform transformOverlayCanvas = Main.RequestUI_GetCanvasOverlay().transform;

                    mTaskGetOpening = Main.RequestAssetAysnc_Instantiate(EAsset.OpeningGame, transformCameraCanvas);
                    mTaskGetUITitle = Main.RequestAssetAysnc_Instantiate(EAsset.UITitle, transformOverlayCanvas);
                    break;
                case 3:
                    if (false == mTaskGetOpening.IsCompletedSuccessfully
                        || false == mTaskGetUITitle.IsCompletedSuccessfully)
                    {
                        return index;
                    }

                    mParentTransform = mTaskGetOpening.Result.transform;
                    break;

                /* play logo */
                case 4:
                    IEOpeningLogo logo = new IEOpeningLogo(mParentTransform.GetChild(0));
                    mCoroutineHandler = new CCoroutine<IEOpeningLogo>(logo);
                    Main.AddCoroutine(mCoroutineHandler);
                    break;
                case 5:
                    if (false == mCoroutineHandler.IsDone)
                    {
                        return index;
                    }
                    break;

                /* play demo */
                case 6:
                    IEOpeningDemo demo = new IEOpeningDemo(mParentTransform.GetChild(1));
                    mCoroutineHandler = new CCoroutine<IEOpeningDemo>(demo);
                    Main.AddCoroutine(mCoroutineHandler);
                    break;
                case 7:
                    if (false == mCoroutineHandler.IsDone)
                    {
                        return index;
                    }
                    break;

                /* play title */
                case 8:
                    IEOpeningTitle title = new IEOpeningTitle(mParentTransform.GetChild(2));
                    mCoroutineHandler = new CCoroutine<IEOpeningTitle>(title);
                    Main.AddCoroutine(mCoroutineHandler);
                    break;
                case 9:
                    if (false == mCoroutineHandler.IsDone)
                    {
                        return index;
                    }
                    break;

                /* set ui title */
                case 10:
                    global::Main.UIMgr.Pop(EUIType.Title, true);
                    break;

                /* dispose */
                default:
                    mTaskGetOpening.Dispose();
                    mTaskGetUITitle.Dispose();

                    mLoadAsyncOper   = null;
                    mCurtainCanvas   = null;
                    mTaskGetOpening  = null;
                    mTaskGetUITitle  = null;
                    mParentTransform = null;
                    mCoroutineHandler       = null;

                    GC.Collect(0, GCCollectionMode.Forced);
                    return -1;
            }

            return index + 1;
        }


        public IEOpeningScene(CanvasGroup curtain)
        {
            mCurtainCanvas = curtain;
            mCurtainCanvas.gameObject.SetActive(true);
        }
    }
    public class IEOpeningLogo : IRoutineUpdater
    {
        private readonly Image mLogoImage;
        private float mWaitTime;
        private float mAlpha;
        private int   mState;

        private Main_ Main { get => Main_.Instance; }

        public int MoveNext(int index)
        {
            /* input */
            if (0 == mState
                && true == Main.InputReserved.Contains(EInput.ENTER | EInput.ACTION))
            {
                mAlpha = 1f;
                mLogoImage.color = new Color(1f, 1f, 1f, mAlpha);

                index = mState = 2;
            }

            /* move next */
            switch (index)
            {
                case 0:
                    if (mAlpha < 1)
                    {
                        mAlpha += Time.deltaTime * 0.75f;
                        mLogoImage.color = new Color(1f, 1f, 1f, mAlpha);
                        return index;
                    }
                    mAlpha = 1f;
                    break;
                case 1:
                    if (mWaitTime < 1f)
                    {
                        mWaitTime += Time.deltaTime;
                        return index;
                    }
                    break;
                case 2:
                    if (mAlpha > 0)
                    {
                        mAlpha -= Time.deltaTime * 2f;
                        mLogoImage.color = new Color(1f, 1f, 1f, mAlpha);
                        return index;
                    }
                    break;
                default:
                    return -1;
            }
            return mState = index + 1;
        }

        public IEOpeningLogo(Transform transform)
        {
            transform.gameObject.SetActive(true);
            mLogoImage = transform.GetComponent<Image>();

            mAlpha = 0;
            mWaitTime = 0;
            mState = 0;
        }
    }
    public class IEOpeningDemo : IRoutineUpdater
    {
        public int MoveNext(int index)
        {
            return -1;
        }
        public IEOpeningDemo(Transform transform)
        {
            Debug.Log("Need to dev: Play Demo");
        }
    }
    public class IEOpeningTitle : IRoutineUpdater
    {
        private readonly Image[] mImages; //logo_upper, logo_lower, flash
        private readonly RectTransform[] mRects;
        private readonly Vector2[] mPositions;

        private readonly float mLogoSpeed  = 4000f;
        private readonly float mFlashSpeed = 5f;
        private readonly float mMovingTime = 0.75f;
        private readonly float mDist;

        private float mPassedTime = 0f;
        private float mAlpah = 0;

        public IEOpeningTitle(Transform transform)
        {
            mRects = new RectTransform[2];
            mPositions = new Vector2[2];
            mDist = mLogoSpeed * mMovingTime;

            //all images.alpha = 0f;
            mImages = transform.GetComponentsInChildren<Image>();
            for (int i = 0; i < mImages.Length; ++i)
            {
                mImages[i].color = new Color(1f, 1f, 1f, 0f);
            }

            //logo_upper
            mRects[0] = mImages[0].GetComponent<RectTransform>();
            mRects[0].anchoredPosition = new Vector3(mRects[0].anchoredPosition.x, mRects[0].anchoredPosition.y + mDist);
            mPositions[0] = mRects[0].anchoredPosition;

            //logo_lower
            mRects[1] = mImages[1].GetComponent<RectTransform>();
            mRects[1].anchoredPosition = new Vector3(mRects[1].anchoredPosition.x, mRects[1].anchoredPosition.y - mDist);
            mPositions[1] = mRects[1].anchoredPosition;
        }
        public int MoveNext(int index)
        {
            switch (index)
            {
                case 0:
                    mImages[0].color = mImages[1].color = new Color(1f, 1f, 1f, 1f);
                    break;
                case 1:
                    float ratio = mPassedTime / mMovingTime;
                    mRects[0].anchoredPosition = new Vector3(mPositions[0].x, mPositions[0].y - mDist * ratio);
                    mRects[1].anchoredPosition = new Vector3(mPositions[1].x, mPositions[1].y + mDist * ratio);

                    if (mMovingTime > mPassedTime)
                    {
                        mPassedTime += Time.deltaTime;
                        return index;
                    }
                    mAlpah = 0;
                    break;
                case 2:
                    //flash
                    mAlpah += Time.deltaTime * mFlashSpeed;
                    mImages[2].color = new Color(1, 1, 1, mAlpah);
                    if (mAlpah < 1f)
                    {
                        return index;
                    }
                    mAlpah = 1f;
                    break;
                case 3:
                    mAlpah -= Time.deltaTime * (mFlashSpeed * 0.6f);
                    mImages[2].color = new Color(1, 1, 1, mAlpah);
                    if (mAlpah > 0f)
                    {
                        return index;
                    }
                    break;
                default:
                    return -1;
            }

            return index + 1;
        }
    }
    #endregion


}
