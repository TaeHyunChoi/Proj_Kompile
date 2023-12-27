using System;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public abstract class ContentOpening : MonoBehaviour
{
    protected int state;
    protected bool canSkip;
    public abstract void Play();
    public abstract void Next();
    public void NextState()
    {
        if (canSkip)
        {
            ++state;
            canSkip = false;
        }
    }
}
public class OnTitle : MonoBehaviour
{
    //Inherit: ContentOpening
    private class OpeningLogo : ContentOpening
    {
        private Image   logoImage;

        private float   endTime;
        private float   waitTime;

        public override void Play()
        {
            canSkip = true;
            enabled = false;
            gameObject.SetActive(false);

            logoImage = transform.GetComponent<Image>();
            endTime = 0f;
            waitTime = 1.5f;
            state = -1;

            Next();
        }
        public override void Next()
        {
            switch (++state)
            {
                case 0:
                    gameObject.SetActive(true);
                    enabled = true;
                    break;
                case 1:
                    endTime = Time.time + waitTime;
                    break;
                case 2:
                    logoImage.color = Color.white;
                    break;
                case 3:
                    enabled = false;
                    gameObject.SetActive(false);
                    instance.NextSequence();
                    break;
            }
        }
        private void Update()
        {
            switch (state)
            {
                case 0:
                    float cValue = logoImage.color.r + Time.deltaTime * Value.FADE_SPEED;
                    logoImage.color = new Color(cValue, cValue, cValue, 1f);
                    if (cValue >= 1)
                    {
                        Next();
                    }
                    break;
                case 1:
                    if (endTime < Time.time)
                    {
                        Next();
                    }
                    break;
                case 2:
                    cValue = logoImage.color.r - Time.deltaTime * Value.FADE_SPEED;
                    logoImage.color = new Color(cValue, cValue, cValue, 1f);
                    if (cValue <= 0)
                    {
                        Next();
                    }
                    break;
            }
        }
    }
    private class OpeningDemo : ContentOpening
    {
        public override void Play()
        {
            canSkip = true;
            enabled = false;
            gameObject.SetActive(false);

            state = 0;

            Next();
        }
        public override void Next()
        {
            switch (state++)
            {
                case 0:
                    Debug.Log("Not yet Play Demo => OnOpening.MoveNext();");
                    instance.NextSequence();
                    break;
            }
        }
        private void Update()
        {
            
        }
    }
    private class OpeningTitle : ContentOpening
    {
        private Image[] images;
        private RectTransform[] rect;
        private Vector2[] pos;

        private float logoSpeed  = 3000f;
        private float passedtime = 0f;
        private float movingTime = 0.75f;
        private float flashSpeed = 5f;
        private float dist;

        public override void Play()
        {
            state = -1;
            canSkip = false;
            enabled = false;
            gameObject.SetActive(false);

            images = transform.GetComponentsInChildren<Image>();
            rect = new RectTransform[2];
            pos = new Vector2[2];
            dist = logoSpeed * movingTime;

            rect[0] = images[1].GetComponent<RectTransform>();
            rect[0].anchoredPosition = new Vector3(rect[0].anchoredPosition.x, rect[0].anchoredPosition.y + dist);
            pos[0] = rect[0].anchoredPosition;

            rect[1] = images[2].GetComponent<RectTransform>();
            rect[1].anchoredPosition = new Vector3(rect[1].anchoredPosition.x, rect[1].anchoredPosition.y - dist);
            pos[1] = rect[1].anchoredPosition;

            images[3].enabled = false;
            Next();
        }
        public override void Next()
        {
            switch (++state)
            {
                case 0:
                    passedtime = 0;
                    enabled = true;
                    gameObject.SetActive(true);
                    break;
                case 1:
                    images[3].enabled = true;
                    images[3].color = new Color(1, 1, 1, 0);
                    break;
                case 2:
                    images[3].color = new Color(1, 1, 1, 1);
                    break;
                case 3:
                    enabled = false;
                    images[3].enabled = false;
                    instance.NextSequence();
                    break;
            }
        }
        private void Update()
        {
            switch (state)
            {
                case 0:
                    float ratio = passedtime / movingTime;
                    rect[0].anchoredPosition = new Vector3(pos[0].x, pos[0].y - dist * ratio);
                    rect[1].anchoredPosition = new Vector3(pos[1].x, pos[1].y + dist * ratio);

                    if (passedtime > movingTime)
                    {
                        rect = null; 
                        pos = null;

                        Next();
                    }
                    else
                    {
                        passedtime += Time.deltaTime;
                    }

                    break;
                case 1:
                    float alpha = images[3].color.a + Time.deltaTime * flashSpeed;
                    images[3].color = new Color(1, 1, 1, alpha);
                    if (alpha >= 1)
                    {
                        Next();
                    }

                    break;
                case 2:
                    alpha = images[3].color.a - Time.deltaTime * (flashSpeed * 0.6f);
                    images[3].color = new Color(1, 1, 1, alpha);
                    if (alpha <= 0)
                    {
                        Next();
                    }

                    break;
            }
        }
    }
    

    private static OnTitle instance;
    private ContentOpening current;
    private int state = 0;


    public static async Task<OnTitle> InitAsync(Transform canvas_ui)
    {
        try
        {
            GameObject obj = await AssetManager.InstantiateAsync("OpeningFade", canvas_ui, true);
            instance = obj.AddComponent<OnTitle>();
        }
        catch (Exception ex)
        {
            Debug.LogError("Error loading assets: " + ex.Message);
            return null;
        }

        return instance;
    }
    private void Awake()
    {
        state = 0;
    }
    private void Start()
    {
        Main.GetInputManager().Set(ContentType.Title);
        NextSequence();
    }
    private void NextSequence()
    {
        switch (state++)
        {
            case 0:
                OpeningLogo contentLogo = transform.GetChild(0).AddComponent<OpeningLogo>();
                current = contentLogo.GetComponent<ContentOpening>();
                current.Play();
                break;
            case 1:
                OpeningDemo contentDemo = transform.GetChild(1).AddComponent<OpeningDemo>();
                current = contentDemo.GetComponent<ContentOpening>();
                current.Play();         
                break;
            case 2:
                OpeningTitle contentTitle = transform.GetChild(2).AddComponent<OpeningTitle>();
                current = contentTitle.GetComponent<ContentOpening>();
                current.Play();
                break;
            case 3:
                UIBase uiTitleMenu = Main.GetUIManager().GetUI(UIType.Title);
                uiTitleMenu.Open();
                break;
            case 4:
                //Loading Curtain; Load Field; ...
                //Destroy(this.gameObject);
                break;
        }
    }
    private void OnDestroy()
    {
        AssetManager.ReleaseAsset(gameObject.GetInstanceID());
    }

    public static void Input(int input)
    {
        if (IDxInput.Compare(input, IDxInput.ENTER))
        {
            instance.current.NextState();
        }
    }
}