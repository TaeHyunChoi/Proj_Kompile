using UnityEngine;
using UnityEngine.UI;
using Index;

public partial class OnOpening // Coroutine
{
    private class OpeningLogo : IRoutineUpdater, IInputHandler
    {
        private Image imageLogo;
        private float wait;
        private float alpha;
        private int state;

        public int MoveNext(int index)
        {
            if (state != index)
            {
                index = state;
            }

            switch (index)
            {
                case 0:
                    if (alpha < 1)
                    {
                        alpha += Time.deltaTime * 0.75f;
                        imageLogo.color = new Color(1f, 1f, 1f, alpha);
                        return index;
                    }
                    alpha = 1f;
                    break;
                case 1:
                    if (wait < 1f)
                    {
                        wait += Time.deltaTime;
                        return index;
                    }
                    break;
                case 2:
                    if (alpha > 0)
                    {
                        alpha -= Time.deltaTime * 2f;
                        imageLogo.color = new Color(1f, 1f, 1f, alpha);
                        return index;
                    }
                    break;
                default:
                    Main.InputMgr.ReleaseInputGetter();
                    instance.Set();
                    return -1;
            }
            return state = index + 1;
        }
        public void Input(int input)
        {
            if (false == IDxInput.Compare(input, IDxInput.ENTER, IDxInput.ACTION))
            {
                return;
            }
            if (0 == state)
            {
                alpha = 1f;
                imageLogo.color = new Color(1f, 1f, 1f, alpha);
                state = 2;
            }
            // In other cases, input doesn`t processed.
        }

        public OpeningLogo(Transform transform)
        {
            transform.gameObject.SetActive(true);
            imageLogo = transform.GetComponent<Image>();
            //imageLogo.color = new Color(1f, 1f, 1f, 0f);
            alpha = 0;
            wait = 0;
            state = 0;

            Main.InputMgr.SetInputGetter(this);
        }
    }
    private class OpeningDemo : IRoutineUpdater, IInputHandler
    {
        public int MoveNext(int index)
        {
            instance.Set();
            return -1;
        }
        public void Input(int input)
        {

        }
        public OpeningDemo(Transform transform)
        {
            Debug.Log("Need to dev: Play Demo");
        }
    }
    private class OpeningTitle : IRoutineUpdater
    {
        private Image[] images; //logo_upper, logo_lower, flash
        private RectTransform[] rect;
        private Vector2[] pos;

        private float logoSpeed = 4000f;
        private float passedtime = 0f;
        private float movingTime = 0.75f;
        private float flashSpeed = 5f;
        private float dist;
        private float alpha = 0;

        public OpeningTitle(Transform transform)
        {
            rect = new RectTransform[2];
            pos = new Vector2[2];
            dist = logoSpeed * movingTime;

            //all images.alpha = 0f;
            images = transform.GetComponentsInChildren<Image>();
            for (int i = 0; i < images.Length; ++i)
            {
                images[i].color = new Color(1f, 1f, 1f, 0f);
            }

            //logo_upper
            rect[0] = images[0].GetComponent<RectTransform>();
            rect[0].anchoredPosition = new Vector3(rect[0].anchoredPosition.x, rect[0].anchoredPosition.y + dist);
            pos[0] = rect[0].anchoredPosition;

            //logo_lower
            rect[1] = images[1].GetComponent<RectTransform>();
            rect[1].anchoredPosition = new Vector3(rect[1].anchoredPosition.x, rect[1].anchoredPosition.y - dist);
            pos[1] = rect[1].anchoredPosition;
        }
        public int MoveNext(int index)
        {
            switch (index)
            {
                case 0:
                    images[0].color = images[1].color = new Color(1f, 1f, 1f, 1f);
                    break;
                case 1:
                    float ratio = passedtime / movingTime;
                    rect[0].anchoredPosition = new Vector3(pos[0].x, pos[0].y - dist * ratio);
                    rect[1].anchoredPosition = new Vector3(pos[1].x, pos[1].y + dist * ratio);

                    if (movingTime > passedtime)
                    {
                        passedtime += Time.deltaTime;
                        return index;
                    }
                    alpha = 0;
                    break;
                case 2:
                    //flash
                    alpha += Time.deltaTime * flashSpeed;
                    images[2].color = new Color(1, 1, 1, alpha);
                    if (alpha < 1f)
                    {
                        return index;
                    }
                    alpha = 1f;
                    break;
                case 3:
                    alpha -= Time.deltaTime * (flashSpeed * 0.6f);
                    images[2].color = new Color(1, 1, 1, alpha);
                    if (alpha > 0f)
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
