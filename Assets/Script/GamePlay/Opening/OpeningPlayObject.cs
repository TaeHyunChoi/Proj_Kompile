namespace Script.GamePlay
{
    using Script.Asset;
    using UnityEngine;
    using UnityEngine.UI;
    using static Script.Data.DataType;

    public class OpeningPlayObject : IngameMonoBehaviourBase
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
            NONE,
            PLAY_LOGO,
            PLAY_DEMO,
            PLAY_TITLE,
            DONE
        }

        [SerializeField] private Image[] images;

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

        // state
        private State state;
        private bool skip;

        // properties
        private float DeltaTime => Time.deltaTime;
        public override PrefabID PrefabID => PrefabID.OpeningPlayObject;

        private void Awake()
        {
            state = State.NONE;
            skip = false;

            Color initColor = new Color(1f, 1f, 1f, 0f);
            images[(int)ImageType.COMPANY_LOGO].color = initColor;
            images[(int)ImageType.TITLE_LOGO_LOWER].color = initColor;
            images[(int)ImageType.TITLE_LOGO_UPPER].color = initColor;
            images[(int)ImageType.TITLE_FLASH].color = initColor;
        }

        public async Awaitable PlaySequence()
        {
            state = State.PLAY_LOGO;
            await PlayLogo();

            state = State.PLAY_DEMO;
            await PlayDemo();

            state = State.PLAY_TITLE;
            await PlayTitle();

            state = State.DONE;
        }
        public async Awaitable PlayLogo()
        {
            transform.GetChild(0).gameObject.SetActive(true);

            alpha = 0f;
            while (alpha < 1f 
                   && false == skip)
            {
                alpha += DeltaTime * logoAlphaDelta;
                images[(int)ImageType.COMPANY_LOGO].color = new Color(1f, 1f, 1f, alpha);
                await Awaitable.NextFrameAsync();
            }


            waitTime = 0f;
            while (waitTime < 1f
                   && false == skip)
            {
                waitTime += DeltaTime;
                await Awaitable.NextFrameAsync();
            }

            if (true == skip)
            {
                await Awaitable.NextFrameAsync();                
            }
            
            alpha = 1f;
            while (alpha > 0f)
            {
                alpha -= DeltaTime * logoAlphaDelta;
                images[(int)ImageType.COMPANY_LOGO].color = new Color(1f, 1f, 1f, alpha);
                await Awaitable.NextFrameAsync();
            }
            alpha = 0f;

            skip = false;
        }
        public async Awaitable PlayDemo()
        {
            skip = false;
            
            // not yet developed

            await Awaitable.NextFrameAsync();
        }
        public async Awaitable PlayTitle()
        {
            skip = false;

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

            // move title logo
            float ratio;
            do
            {
                passedTime += DeltaTime;
                ratio = System.Math.Clamp(passedTime / movingTime, 0f, 1f);
                rects[0].anchoredPosition = titleInitPositions[0] - new Vector2(0, movingDist * ratio);
                rects[1].anchoredPosition = titleInitPositions[1] + new Vector2(0, movingDist * ratio);
                await Awaitable.NextFrameAsync();
            }
            while (ratio < 1f);


            // flash
            while (alpha < 1f)
            {
                alpha += DeltaTime * flashDelta;
                images[(int)ImageType.TITLE_FLASH].color = new Color(1f, 1f, 1f, alpha);
                await Awaitable.NextFrameAsync();
            };
            alpha = 1f;

            while (waitTime < 0.25f)
            {
                waitTime += DeltaTime;
                await Awaitable.NextFrameAsync();
            };

            while (alpha > 0f)
            {
                alpha -= DeltaTime * flashDelta * 1.125f;
                images[(int)ImageType.TITLE_FLASH].color = new Color(1f, 1f, 1f, alpha);
                await Awaitable.NextFrameAsync();
            };
        }

        public bool SkipSequence(Data.DataType.InputState inputState)
        {
            switch (state)
            {
                case State.PLAY_LOGO:
                case State.PLAY_DEMO:
                    if (true == inputState.IsDown(IDxInput.SELECT_ALL))
                    {                    
                        skip = true;
                        return true; // 입력 소모
                    }
                    break;
                default:
                    break;
            }

            return false;
        }
    }
}