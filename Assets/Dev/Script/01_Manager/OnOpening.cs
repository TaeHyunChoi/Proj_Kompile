using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public abstract class ContentOpening : MonoBehaviour
{
    public abstract void Play();
    public abstract void Next();
}
public class OnOpening : MonoBehaviour
{
    private class OpeningLogo : ContentOpening
    {
        private Image   logoImage;
        private int     state;
        private float   endTime;
        private float   waitTime;

        public override void Play()
        {
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
                    opening.NextSequence();
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
        private int state;
        public override void Play()
        {
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
                    opening.NextSequence();
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
        private int state;

        private float logoSpeed = 3000f;
        private float passedtime = 0f;
        private float movingTime = 1f;
        private float flashSpeed = 5f;

        public override void Play()
        {
            enabled = false;
            gameObject.SetActive(false);

            state = -1;
            images = transform.GetComponentsInChildren<Image>();
            
            Vector2 pos;
            pos = images[1].transform.position;
            images[1].transform.position = pos + Vector2.up * logoSpeed * movingTime;
            pos = images[2].transform.position;
            images[2].transform.position = pos + Vector2.down * logoSpeed * movingTime;
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
                    opening.NextSequence();
                    break;
            }
        }
        private void Update()
        {
            switch (state)
            {
                case 0:
                    Vector2 pos;
                    pos = images[1].transform.position;
                    images[1].transform.position = pos + Vector2.down * logoSpeed * Time.deltaTime;
                    pos = images[2].transform.position;
                    images[2].transform.position = pos + Vector2.up * logoSpeed * Time.deltaTime;

                    if (passedtime > movingTime)
                    {
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

    private static OnOpening opening;
    private ContentOpening current;
    private int state = 0;

    public static async Task<bool> InitAsync(Transform canvas_ui)
    {
        try
        {
            GameObject obj = await AssetManager.InstantiateAsync("OpeningFade", canvas_ui);
            opening = obj.AddComponent<OnOpening>();
        }
        catch (Exception ex)
        {
            Debug.LogError("Error loading assets: " + ex.Message);
            return false;
        }

        return true;
    }
    private void Awake()
    {
        state = 0;
    }
    private void Start()
    {
        NextSequence();
    }
    private void NextSequence()
    {
        switch (state++)
        {
            case 0:
                OpeningLogo logo = transform.GetChild(0).AddComponent<OpeningLogo>();
                current = logo.GetComponent<ContentOpening>();
                current.Play();
                break;
            case 1:
                OpeningDemo demo = transform.GetChild(1).AddComponent<OpeningDemo>();
                current = demo.GetComponent<ContentOpening>();
                current.Play();         
                break;
            case 2:
                OpeningTitle title = transform.GetChild(2).AddComponent<OpeningTitle>();
                current = title.GetComponent<ContentOpening>();
                current.Play();
                break;
            case 3:
                /* call title ui */  
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
        if (IDx.AnyKeyHold(input))
        {
            Debug.Log("HOLD");
            return;
        }

        Debug.Log("해치웠나?");
        //어떤 입력을 받아서 어떻게 처리할 것인가
        //언제 입력이 가능한가(불가능한가)

        //아 여러 번 호출되는 것도 막아야 하네? 흠
        //HOLD는 안된다. 
        //NextSequence()가 호출되기 전까지는 입력을 막아야 하네.
        opening.current.Next();
    }
}