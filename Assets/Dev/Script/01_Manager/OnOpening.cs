using System;
using System.Collections;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class OnOpening : MonoBehaviour
{
    private class OpeningLogo : MonoBehaviour, ICoroutine
    {
        private Image               logoFaded;
        private ICoroutine.MoveDele UpdateLogo;

        private int   status;
        private float endTime;
        private float waitTime;

        private void Awake()
        {
            enabled = false;
        }
        public void Play(OnOpening opening)
        {
            waitTime = 1.5f;
            MoveNext(status = 0);
        }
        public void MoveNext(int index)
        {
            switch (index)
            {
                case 0:
                    logoFaded = transform.GetComponent<Image>();
                    logoFaded.color = Color.black;
                    UpdateLogo = FadeIn;
                    enabled = true;
                    break;
                case 1:
                    endTime = Time.time + waitTime;
                    UpdateLogo = Wait;
                    break;
                case 2:
                    logoFaded.color = Color.white;
                    UpdateLogo = FadeOut;
                    break;
                case 3:
                    enabled = false;
                    instance.MoveNext();
                    break;
            }
            status += 1;
        }
        public void FadeIn()
        {
            float cValue = logoFaded.color.r + Time.deltaTime * Value.FADE_SPEED;
            logoFaded.color = new Color(cValue, cValue, cValue, 1f);
            if (cValue >= 1)
                MoveNext(status);
        }
        public void Wait()
        {
            if (endTime <= Time.time)
            {
                MoveNext(status);
            }
        }
        public void FadeOut()
        {
            float cValue = logoFaded.color.r - Time.deltaTime * Value.FADE_SPEED;
            logoFaded.color = new Color(cValue, cValue, cValue, 1f);
            if (cValue <= 0)
                MoveNext(status);
        }


        private void Update()
        {
            UpdateLogo();
        }
    }
    private class OpeningDemo : MonoBehaviour, ICoroutine
    {
        private void Awake()
        {
            enabled = false;
        }
        public void MoveNext(int index)
        {

        }
        public void Play(OnOpening opening)
        { 
            
        }
    }
    private class OpeningTitle : MonoBehaviour, ICoroutine
    {
        private void Awake()
        {
            enabled = false;
        }
        public void MoveNext(int index)
        {

        }
        public void Play(OnOpening opening)
        {

        }
    }

    private static OnOpening instance;

    private OpeningLogo logo;
    private OpeningDemo demo;
    private OpeningTitle title;

    private int current = 0;

    //static 개불편...
    public static async Task<bool> InitAsync(Transform canvas_ui)
    {
        try
        {
            GameObject obj = await AssetManager.InstantiateAsync("OpeningFade", canvas_ui);
            instance = obj.AddComponent<OnOpening>();
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
        switch (current)
        {
            case 0: 
                logo.Play(this); 
                break;
            case 1:
                logo = null;
                demo.Play(this); 
                break;
            case 2:
                demo = null;
                title.Play(this); 
                break;
        }

        current += 1;
    }
}