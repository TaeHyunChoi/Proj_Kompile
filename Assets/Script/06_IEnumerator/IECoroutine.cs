using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static Index.IDxInput;
using UnityEngine.Assertions;

namespace IECoroutine
{
    /* scene : loading curtain */
    public class IELoadingCurtainOn : IRoutineUpdater
    {
        private readonly CanvasGroup mLoadingCurtain;
        public IELoadingCurtainOn(CanvasGroup loadingCurtain)
        {
            mLoadingCurtain = loadingCurtain;
            mLoadingCurtain.gameObject.SetActive(true);
            mLoadingCurtain.alpha = 0f;
        }

        public int MoveNext(int index)
        {
            switch (index)
            {
                case 0:
                    mLoadingCurtain.alpha += Time.deltaTime;
                    if (1 > mLoadingCurtain.alpha)
                    {
                        return index;
                    }
                    mLoadingCurtain.alpha = 1f;
                    break;
                default:
                    return -1;
            }

            return index + 1;
        }
    }
    public class IELoadingCurtainOff : IRoutineUpdater
    {
        private readonly CanvasGroup mLoadingCurtain;

        public IELoadingCurtainOff(CanvasGroup loadingCurtain)
        {
            mLoadingCurtain = loadingCurtain;
            //mLoadingCurtain.gameObject.SetActive(true);
            mLoadingCurtain.alpha = 1f;
        }

        public int MoveNext(int index)
        {
            switch (index)
            {
                case 0:
                    mLoadingCurtain.alpha -= Time.deltaTime;
                    if (0 < mLoadingCurtain.alpha)
                    {
                        return index;
                    }
                    mLoadingCurtain.alpha = 0f;
                    mLoadingCurtain.gameObject.SetActive(false);
                    break;
                default:
                    return -1;
            }

            return index + 1;
        }
    }

    /* scene : load async*/
    public class IELoadScene : IRoutineUpdater
    {
        private readonly AsyncOperation mLoadAsyncOper;

        public IELoadScene(string mapCode)
        {
            mLoadAsyncOper = SceneManager.LoadSceneAsync(mapCode, LoadSceneMode.Single);
        }
        public int MoveNext(int index)
        {
            switch (index)
            {
                case 0:
                    if (false == mLoadAsyncOper.isDone)
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
    public class IEOpeningScene : IRoutineUpdater
    {
        private CanvasGroup    mCurtainCanvas;

        private Task<GameObject> mTaskGetOpening;
        private Task<GameObject> mTaskGetUITitle;

        private Transform mParentTransform;
        private IERoutine mPlayOpeningRoutine;
        private Main_ mMain { get => Main_.Instance; }

        public int MoveNext(int index)
        {
            switch (index)
            {
                /* load prefabs */
                case 0:
                    Transform transformCameraCanvas = mMain.UI_GetCanvasCamera().transform;
                    Transform transformOverlayCanvas = mMain.UI_GetCanvasOverlay().transform;

                    mTaskGetOpening = mMain.Asset_InstantiateAysnc(EAsset.OpeningGame, transformCameraCanvas);
                    mTaskGetUITitle = mMain.Asset_InstantiateAysnc(EAsset.UITitle, transformOverlayCanvas);
                    break;
                case 1:
                    if (false == mTaskGetOpening.IsCompletedSuccessfully
                        || false == mTaskGetUITitle.IsCompletedSuccessfully)
                    {
                        return index;
                    }

                    mParentTransform = mTaskGetOpening.Result.transform;
                    for (int i = 0; i < mParentTransform.childCount; ++i)
                    {
                        mParentTransform.GetChild(i).gameObject.SetActive(false);
                    }

                    mTaskGetUITitle.Result.SetActive(false);
                    mCurtainCanvas.gameObject.SetActive(false);
                    break;
                case 2:
                    IEOpeningLogo  logo  = new IEOpeningLogo(mParentTransform.GetChild(0));
                    IEOpeningDemo  demo  = new IEOpeningDemo(mParentTransform.GetChild(1));
                    IEOpeningTitle title = new IEOpeningTitle(mParentTransform.GetChild(2));

                    mPlayOpeningRoutine = new IERoutine(logo, demo, title);
                    mMain.AddCoroutine(mPlayOpeningRoutine);
                    break;
                case 3:
                    if (false == mPlayOpeningRoutine.IsDone)
                    {
                        return index;
                    }

                    IEUITItle uititle = new IEUITItle(mTaskGetUITitle.Result);
                    mMain.AddCoroutine(new IERoutine(uititle));
                    break;
                default:
                    mTaskGetOpening.Dispose();
                    mTaskGetUITitle.Dispose();

                    mCurtainCanvas      = null;
                    mTaskGetOpening     = null;
                    mTaskGetUITitle     = null;
                    mParentTransform    = null;
                    mPlayOpeningRoutine = null;

                    GC.Collect(0, GCCollectionMode.Optimized);
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

    /* content: opening */
    public class IEOpeningLogo : IRoutineUpdater
    {
        private readonly Image mLogoImage;
        private float mWaitTime;
        private float mAlpha;
        private int mState;

        public int MoveNext(int index)
        {
            /* input */
            if (0 == mState
                && true == Main_.Input_Get().Contains(EInput.ENTER | EInput.ACTION))
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
            transform.gameObject.SetActive(true);
            Debug.Log("Need to dev: Play Demo");
        }
    }
    public class IEOpeningTitle : IRoutineUpdater
    {
        private readonly Image[] mImages; //logo_upper, logo_lower, flash
        private readonly RectTransform[] mRects;
        private readonly Vector2[] mPositions;

        private readonly float mLogoSpeed = 4000f;
        private readonly float mFlashSpeed = 5f;
        private readonly float mMovingTime = 0.75f;
        private readonly float mDist;

        private float mPassedTime = 0f;
        private float mAlpah = 0;

        public IEOpeningTitle(Transform transform)
        {
            transform.gameObject.SetActive(true);

            mRects = new RectTransform[2];
            mPositions = new Vector2[2];
            mDist = mLogoSpeed * mMovingTime;
            mImages = transform.GetComponentsInChildren<Image>();

            //logo_upper
            mRects[0] = mImages[0].GetComponent<RectTransform>();
            mRects[0].anchoredPosition = new Vector3(mRects[0].anchoredPosition.x, mRects[0].anchoredPosition.y + mDist);
            mPositions[0] = mRects[0].anchoredPosition;
            mImages[0].color = new Color(1f, 1f, 1f, 1f);

            //logo_lower
            mRects[1] = mImages[1].GetComponent<RectTransform>();
            mRects[1].anchoredPosition = new Vector3(mRects[1].anchoredPosition.x, mRects[1].anchoredPosition.y - mDist);
            mPositions[1] = mRects[1].anchoredPosition;
            mImages[1].color = new Color(1f, 1f, 1f, 1f);

            //flash
            mImages[2].color = new Color(1f, 1f, 1f, 0f);
        }
        public int MoveNext(int index)
        {
            switch (index)
            {
                case 0:
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
                case 1:
                    // flash on
                    mAlpah += Time.deltaTime * mFlashSpeed;
                    mImages[2].color = new Color(1, 1, 1, mAlpah);
                    if (mAlpah < 1f)
                    {
                        return index;
                    }
                    mAlpah = 1f;
                    break;
                case 2:
                    // flash off
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

    /* ui */
    public class IEUITItle : IRoutineUpdater
    {
        private readonly float ALPHA_MAX = 0.6f;
        private readonly float ALPHA_MIN = 0.3f;
        private readonly float OFFSET_TIME = 0.2f;

        private readonly Image[] mSelectionItems;
        private readonly int mItemCount;

        private float mDeltaTime;
        private float mOffsetTime;
        private int mSelect;

        private Main_ mMain => Main_.Instance;

        public IEUITItle(GameObject obj)
        {
            obj.SetActive(true);
            Transform transform = obj.transform;

            mMain.UI_AddNew(EUIType.Title, obj);
            mMain.UI_Open(EUIType.Title);

            Image[] images = transform.GetChild(0).GetComponentsInChildren<Image>(true);
            mSelectionItems = new Image[images.Length - 1];
            mItemCount = images.Length - 1;
            for (int i = 1; i < images.Length; ++i)
            {
                mSelectionItems[i - 1] = images[i];
            }

            mSelect = 0;
            mOffsetTime = OFFSET_TIME;
        }
        public int MoveNext(int index)
        {
            index = Input(Main_.Input_Get(), index);

            switch (index)
            {
                case 0:
                    if (mSelectionItems[mSelect].color.a <= ALPHA_MIN)
                    {
                        mDeltaTime = Time.deltaTime;
                    }
                    else if (mSelectionItems[mSelect].color.a >= ALPHA_MAX)
                    {
                        mDeltaTime = -Time.deltaTime;
                    }

                    mSelectionItems[mSelect].color += new Color(0, 0, 0, mDeltaTime * 0.75f);
                    break;
                default:
                    return -1;
            }

            return index;
        }
        public int Input(EInput input, int index)
        {
            if (true == input.Contains(EInput.ENTER | EInput.ACTION))
            {
                SetItemColor(mSelect, ALPHA_MAX);

                switch (mSelect)
                {
                    case 0:
                        Debug.Log("New game For Test (map code: 900)");
                        mMain.MapScene_EnterField(900);
                        break;
                    case 1:
                        Debug.Log("Saved Data List");
                        break;
                    case 2:
                        Debug.Log("Option window");
                        break;
                    case 3:
#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
                        UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
                        break;
                    default:
                        Assert.IsFalse(false, $"Wrong Input Index: IEUITItle.Input({mSelect})");
                        break;
                }

                return -1;
            }
            if (true == input.Contains(EInput.CANCEL))
            {
                Debug.Log("Cancel");
                mOffsetTime = 0;
            }

            mOffsetTime -= Time.deltaTime;
            if (0 > mOffsetTime) 
            { 
                mOffsetTime = 0; 
            }

            // block successive input
            if (true == input.Contains(EInput.UP_HOLD | EInput.DOWN_HOLD))
            {
                if (0 < mOffsetTime) 
                { 
                    return index; 
                }
            }

            bool isUp   = input.Contains(EInput.UP | EInput.UP_HOLD);
            bool isDown = input.Contains(EInput.DOWN | EInput.DOWN_HOLD);

            if (true == isUp || true == isDown)
            {
                // prev
                SetItemColor(mSelect, 0f);

                // loop index (0 ~ 3)
                mSelect = isUp ? mSelect - 1 : mSelect + 1;
                mSelect = (mSelect + mItemCount) % mItemCount;

                // next
                SetItemColor(mSelect, ALPHA_MIN);

                // reset-offset
                mOffsetTime = OFFSET_TIME;
            }

            //if (true == input.Contains(EInput.UP | EInput.UP_HOLD))
            //{
            //    SetItemColor(mSelect, 0f); //prev
            //    mSelect = (mSelect - 1 + mItemCount) % mItemCount;
            //    SetItemColor(mSelect, ALPHA_MIN); //next

            //    mOffsetTime = OFFSET_TIME;
            //}
            //if (true == input.Contains(EInput.DOWN | EInput.DOWN_HOLD))
            //{
            //    SetItemColor(mSelect, 0f);
            //    mSelect = (mSelect + 1 + mItemCount) % mItemCount;
            //    SetItemColor(mSelect, ALPHA_MIN);

            //    mOffsetTime = OFFSET_TIME;
            //}

            return index;
        }
        private void SetItemColor(int index, float alpha)
        {
            Color target = mSelectionItems[index].color;
            mSelectionItems[index].color = new Color(target.r, target.g, target.b, alpha);
        }
    }
    public class IEClearUI : IRoutineUpdater
    {
        public IEClearUI(EUIGroup exceptGroupType)
        {
            Main_.Instance.UI_Clear(exceptGroupType);
        }
        public int MoveNext(int index)
        {
            return -1;
        }
    }

    /* asset */
    public class IEClearAllAsset : IRoutineUpdater
    {
        public int MoveNext(int index)
        {
            Main_.Instance.Asset_ClearAll();
            return -1;
        }
    }
}
