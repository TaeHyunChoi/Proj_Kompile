using UnityEngine;
using UnityEngine.UI;
using static Index.IDxInput;

public partial class OnOpening // Coroutine
{
    private class OpeningLogo : IRoutineUpdater, IInputHandler
    {
        private Image mLogoImage;
        private float mWaitTime;
        private float mAlpha;
        private int   mState;

        public int MoveNext(int index)
        {
            if (mState != index)
            {
                index = mState;
            }

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
                    Main.InputMgr.Updater = null;
                    instance.Set();
                    break;
                default:
                    return -1;
            }
            return mState = index + 1;
        }
        public void Input(EInput input)
        {
            if (false == Compare(input, EInput.ENTER, EInput.ACTION))
            {
                return;
            }

            if (0 == mState)
            {
                mAlpha = 1f;
                mLogoImage.color = new Color(1f, 1f, 1f, mAlpha);

                mState = 2;
            }
        }

        public OpeningLogo(Transform transform)
        {
            transform.gameObject.SetActive(true);
            mLogoImage = transform.GetComponent<Image>();
            //imageLogo.color = new Color(1f, 1f, 1f, 0f);
            mAlpha = 0;
            mWaitTime = 0;
            mState = 0;

            Main.InputMgr.Updater = this;
        }
    }
    private class OpeningDemo : IRoutineUpdater, IInputHandler
    {
        public int MoveNext(int index)
        {
            instance.Set();
            return -1;
        }
        public void Input(EInput input)
        {

        }
        public OpeningDemo(Transform transform)
        {
            Debug.Log("Need to dev: Play Demo");
        }
    }
    private class OpeningTitle : IRoutineUpdater
    {
        private Image[]         mImages; //logo_upper, logo_lower, flash
        private RectTransform[] mRects;
        private Vector2[]       mPositions;

        private float mLogoSpeed = 4000f;
        private float mPassedTime = 0f;
        private float mMovingTime = 0.75f;
        private float mFlashSpeed = 5f;
        private float mDist;
        private float mAlpah = 0;

        public OpeningTitle(Transform transform)
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
                    instance.Set();
                    return -1;
            }

            return index + 1;
        }
    }
}
