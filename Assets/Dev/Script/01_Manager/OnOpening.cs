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

        private void Awake()
        {
            enabled = false;
        }

        //Inherit
        public override void Play()
        {
            waitTime = 1.5f;
            status = 0;
            MoveNext();
        }
        protected override void MoveNext()
        {
            switch (status)
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
            status += 1;
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
        private void Awake()
        {
            enabled = false;
            status = 0;
        }

        public override void Play()
        {

        }

        protected override void MoveNext()
        {
            switch (status)
            {
                case 0:
                    //타이틀 로고 : 위아래로
                    break;
                case 1:
                    //번쩍 => 타이틀 진짜 로고
                    break;
                case 2:
                    //Call Title UI => 이걸 opening.MoveNext()로 호출하는게 나을 듯
                    opening.MoveNext();
                    break;
            }
        }
    }


    private static OnOpening opening;
    private OpeningLogo  logo;
    private OpeningDemo  demo;
    private OpeningTitle title;

    private int current = 0;

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
    }
    private void Start()
    {
        MoveNext();
    }
    private void MoveNext()
    {
        switch (current++)
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