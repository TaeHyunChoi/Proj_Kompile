using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class OnOpening : MonoBehaviour
{
    private class OpeningLogo : MonoBehaviour, IUpdateSequence
    {
        private Image   logoImage;
        private IUpdateSequence.SequenceDele updateFunc;
        private int     state;
        private float   endTime;
        private float   waitTime;

        public void Play()
        {
            enabled = false;
            gameObject.SetActive(false);

            logoImage = transform.GetComponent<Image>();
            waitTime = 1.5f;
            state = 0;

            Next();
        }
        public void Next()
        {
            switch (state++)
            {
                case 0:
                    updateFunc = FadeIn;

                    gameObject.SetActive(true);
                    enabled = true;
                    break;
                case 1:
                    endTime = Time.time + waitTime;
                    updateFunc = Wait;
                    break;
                case 2:
                    logoImage.color = Color.white;
                    updateFunc = FadeOut;
                    break;
                case 3:
                    enabled = false;
                    gameObject.SetActive(false);

                    opening.NextSequence();

                    logoImage = null;
                    updateFunc = null;
                    break;
            }
        }

        private void FadeIn()
        {
            float cValue = logoImage.color.r + Time.deltaTime * Value.FADE_SPEED;
            logoImage.color = new Color(cValue, cValue, cValue, 1f);
            if (cValue >= 1)
            {
                Next();
            }
        }
        private void Wait()
        {
            if (endTime < Time.time)
            {
                Next();
            }
        }
        private void FadeOut()
        {
            float cValue = logoImage.color.r - Time.deltaTime * Value.FADE_SPEED;
            logoImage.color = new Color(cValue, cValue, cValue, 1f);
            if (cValue <= 0)
            {
                Next();
            }
        }

        private void Update()
        {
            updateFunc();
        }
    }
    private class OpeningDemo : MonoBehaviour, IUpdateSequence
    {
        private int state;
        public void Play()
        {
            enabled = false;
            gameObject.SetActive(false);

            state = 0;

            Next();
        }
        public void Next()
        {
            switch (state++)
            {
                case 0:
                    Debug.Log("Not yet Play Demo => OnOpening.MoveNext();");
                    opening.NextSequence();
                    break;
            }
        }
    }
    private class OpeningTitle : MonoBehaviour, IUpdateSequence
    {
        private Image[] images;
        private IUpdateSequence.SequenceDele updateFunc;
        private int state;

        private float logoSpeed = 2500f;
        private float passedtime = 0f;
        private float movingTime = 1f;
        private float flashSpeed = 5f;

        public void Play()
        {
            enabled = false;
            gameObject.SetActive(false);

            state = 0;
            images = transform.GetComponentsInChildren<Image>();
            
            Vector2 pos;
            pos = images[1].transform.position;
            images[1].transform.position = pos + Vector2.up * logoSpeed * movingTime;
            pos = images[2].transform.position;
            images[2].transform.position = pos + Vector2.down * logoSpeed * movingTime;
            images[3].enabled = false;

            Next();
        }

        public void Next()
        {
            switch (state++)
            {
                case 0:
                    passedtime = 0;
                    updateFunc = MoveTitleLogo;

                    enabled = true;
                    gameObject.SetActive(true);
                    break;
                case 1:
                    images[3].enabled = true;
                    images[3].color = new Color(1, 1, 1, 0);
                    updateFunc = FlashOut;
                    break;
                case 2:
                    images[3].color = new Color(1, 1, 1, 1);
                    updateFunc = FlashIn;
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
            updateFunc();
        }
        private void MoveTitleLogo()
        {
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
        }
        private void FlashOut()
        {
            float alpha = images[3].color.a + Time.deltaTime * flashSpeed;
            images[3].color = new Color(1, 1, 1, alpha);
            if (alpha >= 1)
            {
                Next();
            }
        }
        private void FlashIn()
        {
            float alpha = images[3].color.a - Time.deltaTime * (flashSpeed * 0.6f);
            images[3].color = new Color(1, 1, 1, alpha);
            if (alpha <= 0)
            {
                Next();
            }
        }
    }


    private static OnOpening opening;
    private OpeningLogo  logo;
    private OpeningDemo  demo;
    private OpeningTitle title;

    private int state;

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
        logo  = transform.GetChild(0).AddComponent<OpeningLogo>();
        demo  = transform.GetChild(1).AddComponent<OpeningDemo>();
        title = transform.GetChild(2).AddComponent<OpeningTitle>();
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
                logo.Play(); 
                break;
            case 1:
                demo.Play();
                break;
            case 2:
                title.Play();
                break;
            case 3:
                break;
            case 4:
                //Loading Curtain;
                //Load Field
                //기타 등등

                //Destroy(this.gameObject);
                break;
        }
    }
    private void OnDestroy()
    {
        AssetManager.ReleaseAsset(gameObject.GetInstanceID());
    }
}