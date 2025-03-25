using Script.Index;
using Script.Interface;
using Script.Manager;
using UnityEngine;
using UnityEngine.UI;

public partial class TitleObject : MonoBehaviour, IIngameUpdater, IIngameInput
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

    [SerializeField] private Image[] images;

    private State state;

    // play company logo
    private readonly float logoAlphaDelta = 0.625f;
    private float alpha;
    private float waitTime;

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

        Color initColor = new Color(1f, 1f, 1f, 0f);
        images[(int)ImageType.COMPANY_LOGO].color       = initColor;
        images[(int)ImageType.TITLE_LOGO_LOWER].color   = initColor;
        images[(int)ImageType.TITLE_LOGO_UPPER].color   = initColor;
        images[(int)ImageType.TITLE_FLASH].color        = initColor;

        IngameManager.AddInput(AssetCode.OP_TitleObject, this);
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
                alpha = 0f;
                waitTime = 0f;
                state = State.LOGO_FADE_IN;
                break;

            case State.LOGO_FADE_IN:
                alpha += deltaTime * logoAlphaDelta;
                images[(int)ImageType.COMPANY_LOGO].color = new Color(1f, 1f, 1f, alpha);

                if (1 <= alpha)
                {
                    state = State.LOGO_WAIT;
                    alpha = 1f;
                }
                break;

            case State.LOGO_WAIT:
                if (waitTime < 1f)
                {
                    waitTime += deltaTime;
                }
                state = State.LOGO_FADE_OUT;
                break;

            case State.LOGO_FADE_OUT:
                alpha -= deltaTime * (logoAlphaDelta * 3);
                images[(int)ImageType.COMPANY_LOGO].color = new Color(1f, 1f, 1f, alpha);

                if (0 >= alpha)
                {
                    transform.GetChild(0).gameObject.SetActive(false);
                    state = State.TITLE_INIT;
                }
                break;

            case State.TITLE_INIT:
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
                alpha += deltaTime * flashDelta;
                images[(int)ImageType.TITLE_FLASH].color = new Color(1f, 1f, 1f, alpha);

                if (alpha >= 1f)
                {
                    alpha = 1f;
                    state = State.TITLE_WAIT;
                }
                break;

            case State.TITLE_WAIT:
                if (waitTime < 0.25f)
                {
                    waitTime += deltaTime;
                    break;
                }

                state = State.TITLE_FLASH_OFF;
                break;

            case State.TITLE_FLASH_OFF:
                alpha -= deltaTime * flashDelta * 1.125f;
                images[(int)ImageType.TITLE_FLASH].color = new Color(1f, 1f, 1f, alpha);

                if (alpha < 0)
                {
                    alpha = 0;
                    state = State.END;
                }
                break;

            default:
                IngameManager.RemoveInput(AssetCode.OP_TitleObject);
                //MessageManager.Publish(new Message_t(MessageType.END_OBJECT_PROCESS, AssetIndex.OP_TitleObject));
                MessageManager.Publish(MessageType.END_OBJECT_PROCESS, new OnEndProcess(AssetCode.OP_TitleObject));

                return UpdaterState.SUCCESS;
        }

        return UpdaterState.RUNNING;
    }

    public void Input(IDxInput.InputFlag inputFlag)
    {
        if (false == inputFlag.Contains(IDxInput.InputFlag.ACTION | IDxInput.InputFlag.ENTER))
        {
            return;
        }
        if (State.LOGO_WAIT <= state)
        {
            return;
        }

        alpha = 1f;
        images[(int)ImageType.COMPANY_LOGO].color = new Color(1f, 1f, 1f, alpha);

        waitTime *= 1.5f;
        state = State.LOGO_WAIT;
    }
}