using Script.Index;
using Script.Interface;
using Script.Manager;
using UnityEngine;
using UnityEngine.UI;

public partial class OP_TitleObject
{
    private enum ImageType
    { 
        COMPANY_LOGO,
        // DEMO_PLAY
        TITLE_LOGO_UPPER,
        TITLE_LOGO_LOWER,
        TITLE_FLASH
    }
    private enum State
    {
        NONE = 0,

        // 회사 로고
        LOGO_INIT,
        LOGO_FADE_IN,
        LOGO_WAIT,
        LOGO_FADE_OUT,

        // 데모 플레이
        // DEMO_INIT ...

        // 게임 타이틀
        TITLE_INIT,
        TITLE_MOVE_LOGO,
        TITLE_FLASH_ON,
        TITLE_WAIT,
        TITLE_FLASH_OFF,

        END
    }

}

public partial class OP_TitleObject : MonoBehaviour, IIngameUpdater, IIngameInput
{
    [SerializeField] private Image[] images;

    private State state;

    // play company logo
    private readonly float logoAlphaDelta = 0.625f;
    private float logoAlpha;
    private float logoWaitTime;

    // play title
    private readonly float movingSpeed = 4000f;
    private readonly float movingTime = 0.75f;
    private readonly float flashDelta = 3f;

    private RectTransform[] rects;
    private Vector2[] titleInitPositions;
    private float movingDist;
    private float passedTime;

    private void Awake()
    {
        state = State.NONE;

        transform.GetChild(0).gameObject.SetActive(false);
        transform.GetChild(2).gameObject.SetActive(false);

        IngameManager.AddInput(AssetIndex.OP_TitleObject, this);
        IngameManager.AddUpdater(this);
    }

    public UpdaterState UpdateState()
    {
        float deltaTime = Time.deltaTime;

        switch (state)
        {
            case State.NONE:
                state = State.LOGO_INIT;
                goto case State.LOGO_INIT;

            case State.LOGO_INIT:
                transform.GetChild(0).gameObject.SetActive(true);
                logoAlpha = 0;
                logoWaitTime = 1f;
                state = State.LOGO_FADE_IN;
                break;

            case State.LOGO_FADE_IN:
                logoAlpha += deltaTime * logoAlphaDelta;
                images[(int)ImageType.COMPANY_LOGO].color = new Color(1f, 1f, 1f, logoAlpha);

                if (1 <= logoAlpha)
                {
                    state = State.LOGO_WAIT;
                    logoAlpha = 1f;
                }
                break;

            case State.LOGO_WAIT:
                if (0 < logoWaitTime)
                {
                    logoWaitTime -= deltaTime;
                }
                state = State.LOGO_FADE_OUT;
                break;

            case State.LOGO_FADE_OUT:
                logoAlpha -= deltaTime * (logoAlphaDelta * 3);
                images[(int)ImageType.COMPANY_LOGO].color = new Color(1f, 1f, 1f, logoAlpha);

                if (0 >= logoAlpha)
                {
                    transform.GetChild(0).gameObject.SetActive(false);
                    state = State.TITLE_INIT;
                }
                break;

            case State.TITLE_INIT:
                transform.GetChild(2).gameObject.SetActive(true);
                passedTime = 0f;
                logoAlpha = 0;
                logoWaitTime = 0f;

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

                state = State.TITLE_MOVE_LOGO;
                break;

            case State.TITLE_MOVE_LOGO:
                passedTime += deltaTime;
                float ratio = System.Math.Clamp(passedTime / movingTime, 0f, 1f);
                rects[0].anchoredPosition = titleInitPositions[0] - new Vector2(0, movingDist * ratio);
                rects[1].anchoredPosition = titleInitPositions[1] + new Vector2(0, movingDist * ratio);

                if (ratio >= 1)
                {
                    state = State.TITLE_FLASH_ON;
                }
                break;

            case State.TITLE_FLASH_ON:
                logoAlpha += deltaTime * flashDelta;
                images[(int)ImageType.TITLE_FLASH].color = new Color(1f, 1f, 1f, logoAlpha);

                if (logoAlpha >= 1f)
                {
                    logoAlpha = 1f;
                    state = State.TITLE_WAIT;
                }
                break;

            case State.TITLE_WAIT:
                if (logoWaitTime < 0.25f)
                {
                    logoWaitTime += deltaTime;
                    break;
                }

                state = State.TITLE_FLASH_OFF;
                break;

            case State.TITLE_FLASH_OFF:
                logoAlpha -= deltaTime * flashDelta * 1.125f;
                images[(int)ImageType.TITLE_FLASH].color = new Color(1f, 1f, 1f, logoAlpha);

                if (logoAlpha < 0)
                {
                    logoAlpha = 0;
                    state = State.END;
                }
                break;

            default:
                IngameManager.RemoveInput(AssetIndex.OP_TitleObject);
                MessageManager.Publish(new Message_t(MessageType.END_OBJECT_PROCESS, AssetIndex.OP_TitleObject));
                return UpdaterState.SUCCESS;
        }

        return UpdaterState.RUNNING;
    }

    public void Input(IDxInput.EInputFlag inputFlag)
    {
        if (false == inputFlag.Contains(IDxInput.EInputFlag.ACTION | IDxInput.EInputFlag.ENTER))
        {
            return;
        }
        if (State.LOGO_WAIT <= state)
        {
            return;
        }

        logoAlpha = 1f;
        images[(int)ImageType.COMPANY_LOGO].color = new Color(1f, 1f, 1f, logoAlpha);

        logoWaitTime *= 1.5f;
        state = State.LOGO_WAIT;
    }
}