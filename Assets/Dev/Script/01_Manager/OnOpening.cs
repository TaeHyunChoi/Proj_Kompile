using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class OnOpening : MonoBehaviour
{
    private class OpeningLogo : Content
    {
        private Image logo;
        private float endTime;
        private float waitTime;

        //Inherit
        public override void Play()
        {
            waitTime = 1.5f;
            status = 0;
            gameObject.SetActive(true);
            MoveNext();
        }
        protected override void MoveNext()
        {
            switch (status++)
            {
                case 0:
                    logo = transform.GetComponent<Image>();
                    logo.color = Color.black;
                    updateFunc = FadeIn;
                    enabled = true;
                    break;
                case 1:
                    endTime = Time.time + waitTime;
                    updateFunc = Wait;
                    break;
                case 2:
                    logo.color = Color.white;
                    updateFunc = FadeOut;
                    break;
                case 3:
                    enabled = false;
                    gameObject.SetActive(false);
                    updateFunc = null;
                    opening.MoveNext();
                    break;
            }
        }

        private void FadeIn()
        {
            float cValue = logo.color.r + Time.deltaTime * Value.FADE_SPEED;
            logo.color = new Color(cValue, cValue, cValue, 1f);
            if (cValue >= 1)
            {
                MoveNext();
            }
        }
        private void Wait()
        {
            if (endTime < Time.time)
            {
                MoveNext();
            }
        }
        private void FadeOut()
        {
            float cValue = logo.color.r - Time.deltaTime * Value.FADE_SPEED;
            logo.color = new Color(cValue, cValue, cValue, 1f);
            if (cValue <= 0)
            {
                MoveNext();
            }
        }

        private void Update()
        {
            updateFunc();
        }
    }
    private class OpeningDemo : Content
    {
        private void Awake()
        {
            enabled = false;
            gameObject.SetActive(false);
        }
        public override void Play()
        {
            Debug.Log("Not yet Play Demo => OnOpening.MoveNext();");
            opening.MoveNext();
        }
        protected override void MoveNext()
        {

        }
    }
    private class OpeningTitle : Content
    {
        private Image[] images;
        private float logoSpeed = 2500f;
        private float passedtime = 0f;
        private float movingTime = 1f;
        private float flashSpeed = 5f;

        public override void Play()
        {
            images = transform.GetComponentsInChildren<Image>();

            Vector2 pos;
            pos = images[1].transform.position;
            images[1].transform.position = pos + Vector2.up * logoSpeed * movingTime;
            pos = images[2].transform.position;
            images[2].transform.position = pos + Vector2.down * logoSpeed * movingTime;
            images[3].enabled = false;

            MoveNext();
        }

        protected override void MoveNext()
        {
            switch (status++)
            {
                case 0:
                    gameObject.SetActive(true);
                    enabled = true;
                    passedtime = 0;
                    updateFunc = MoveTitleLogo;
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
                    opening.MoveNext();
                    break;
            }
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
                MoveNext();
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
                MoveNext();
            }
        }
        private void FlashIn()
        {
            float alpha = images[3].color.a - Time.deltaTime * (flashSpeed * 0.6f);
            images[3].color = new Color(1, 1, 1, alpha);
            if (alpha <= 0)
            {
                MoveNext();
            }
        }

        private void Update()
        {
            updateFunc();
        }
    }

    private static OnOpening opening;
    private OpeningLogo  logo;
    private OpeningDemo  demo;
    private OpeningTitle title;

    private int status = 0;

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
    }
    private void Start()
    {
        MoveNext();
    }
    private void MoveNext()
    {
        switch (status++)
        {
            case 0: 
                logo.Play(); 
                break;
            case 1:
                logo = null;
                demo.Play();
                break;
            case 2:
                demo = null;
                title.Play();
                break;
            case 3:
                title = null;
                Debug.Log("Call Title");
                //여기서부터 설계가 필요하군..
                break;
            case 4:
                //Loading Curtain;
                //Load Field
                //기타 등등
                Destroy(this.gameObject);
                break;
        }
    }
    private void OnDestroy()
    {
        AssetManager.ReleaseAsset(gameObject.GetInstanceID());
    }
}