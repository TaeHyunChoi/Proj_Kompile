using Script.Index;
using UnityEngine;
using UnityEngine.UI;

public partial class OP_TitleObject : MonoBehaviour // 공통
{
    private enum ImageType
    { 
        COMPANY_LOGO,
        // DEMO_PLAY
        TITLE_LOGO_UPPER,
        TITLE_LOGO_LOWER,
        TITLE_FLASH
    }
    [SerializeField] private Image[] images;

    private void Awake()
    {
        companyLogoState = PlayCompanyLogoState.INIT;
        titleLogoState   = PlayTitleLogoState.INIT;

        transform.GetChild(0).gameObject.SetActive(false);
        transform.GetChild(2).gameObject.SetActive(false);
    }
}

public partial class OP_TitleObject // PLAY_COMPANY_LOGO
{
    private enum PlayCompanyLogoState
    {
        INIT = 0,
        FADE_IN,
        WAIT,
        FADE_OUT,
    }
    private PlayCompanyLogoState companyLogoState;

    private readonly float alphaDelta = 0.625f;
    private float alpha;
    private float waitTime;

    public IETaskState MoveNext_PlayCompanyLogo()
    {
        float deltaTime = Time.deltaTime;

        switch (companyLogoState)
        {
            case PlayCompanyLogoState.INIT:
                transform.GetChild(0).gameObject.SetActive(true);
                alpha = 0;
                waitTime = 1f;
                ++companyLogoState;
                goto case PlayCompanyLogoState.FADE_IN;

            case PlayCompanyLogoState.FADE_IN:
                alpha += deltaTime * alphaDelta;
                images[(int)ImageType.COMPANY_LOGO].color = new Color(1f, 1f, 1f, alpha);

                if (1 <= alpha)
                {
                    ++companyLogoState;
                    alpha = 1f;
                }
                break;

            case PlayCompanyLogoState.WAIT:
                if (0 < waitTime)
                {
                    waitTime -= deltaTime;
                }

                ++companyLogoState;
                break;

            case PlayCompanyLogoState.FADE_OUT:
                alpha -= deltaTime * (alphaDelta * 3);
                images[(int)ImageType.COMPANY_LOGO].color = new Color(1f, 1f, 1f, alpha);

                if (0 >= alpha)
                {
                    ++companyLogoState;
                }
                break;

            default:
                transform.GetChild(0).gameObject.SetActive(false);
                return IETaskState.SUCCESS;
        }

        return IETaskState.RUNNING;
    }
    public void EndPlayCompnayLogo()
    {
        alpha = 1f;
        images[(int)ImageType.COMPANY_LOGO].color = new Color(1f, 1f, 1f, alpha);

        waitTime *= 1.5f;
        companyLogoState = PlayCompanyLogoState.WAIT;
    }
}

public partial class OP_TitleObject // PLAY_TITLE_LOGO
{
    private enum PlayTitleLogoState
    {
        INIT = 0,
        MOVE_LOGO,
        FLASH_ON,
        WAIT,
        FLASH_OFF
    }
    
    private readonly float movingSpeed = 4000f;
    private readonly float movingTime  = 0.75f;
    private readonly float flashDelta  = 3f;

    private RectTransform[] rects;
    private Vector2[] titleInitPositions;
    private float movingDist;

    private PlayTitleLogoState titleLogoState;
    private float passedTime;

    public IETaskState MoveNext_PlayTitleLogo()
    {
        float deltaTime = Time.deltaTime;

        switch (titleLogoState)
        {
            case PlayTitleLogoState.INIT:
                transform.GetChild(2).gameObject.SetActive(true);
                passedTime = 0f;
                alpha = 0;
                waitTime = 0f;

                // get RectTransform
                rects = new RectTransform[2];
                rects[0] = images[(int)ImageType.TITLE_LOGO_UPPER].GetComponent<RectTransform>();
                rects[1] = images[(int)ImageType.TITLE_LOGO_LOWER].GetComponent<RectTransform>();

                // set anchored position
                movingDist = movingSpeed * movingTime;
                titleInitPositions = new Vector2[2];

                Vector2 anchoredPosition = rects[0].anchoredPosition;
                titleInitPositions[0] = new Vector2(anchoredPosition.x, anchoredPosition.y + movingDist);
                rects[0].anchoredPosition = titleInitPositions[0];

                anchoredPosition = rects[1].anchoredPosition;
                titleInitPositions[1] = new Vector2(anchoredPosition.x, anchoredPosition.y - movingDist);
                rects[1].anchoredPosition = titleInitPositions[1];

                // set color
                images[(int)ImageType.TITLE_LOGO_UPPER].color = new Color(1f, 1f, 1f, 1f);
                images[(int)ImageType.TITLE_LOGO_LOWER].color = new Color(1f, 1f, 1f, 1f);
                images[(int)ImageType.TITLE_FLASH].color = new Color(1f, 1f, 1f, 0f);

                ++titleLogoState;
                break;

            case PlayTitleLogoState.MOVE_LOGO:
                passedTime += deltaTime;
                float ratio = System.Math.Clamp(passedTime / movingTime, 0f, 1f);
                rects[0].anchoredPosition = titleInitPositions[0] - new Vector2(0, movingDist * ratio);
                rects[1].anchoredPosition = titleInitPositions[1] + new Vector2(0, movingDist * ratio);

                if (ratio >= 1)
                {
                    ++titleLogoState;
                }
                break;

            case PlayTitleLogoState.FLASH_ON:
                alpha += deltaTime * flashDelta;
                images[(int)ImageType.TITLE_FLASH].color = new Color(1f, 1f, 1f, alpha);

                if (alpha >= 1f)
                {
                    alpha = 1f;
                    ++titleLogoState;
                }
                break;

            case PlayTitleLogoState.WAIT:
                if (waitTime < 0.25f)
                {
                    waitTime += deltaTime;
                    break;
                }

                ++titleLogoState;
                break;

            case PlayTitleLogoState.FLASH_OFF:
                alpha -= deltaTime * flashDelta * 1.125f;
                images[(int)ImageType.TITLE_FLASH].color = new Color(1f, 1f, 1f, alpha);

                if (alpha < 0)
                {
                    alpha = 0;
                    ++titleLogoState;
                }
                break;

            default:
                return IETaskState.SUCCESS;
        }

        return IETaskState.RUNNING;
    }
}