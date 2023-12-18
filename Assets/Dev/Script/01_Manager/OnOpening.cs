using System;
using System.Collections;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class OnOpening : MonoBehaviour
{
    private class OpeningLogo : MonoBehaviour, IUpdateBySection
    {
        private Image logoFaded;
        private IUpdateBySection.UpdateDele updateFunc;
        private IUpdateBySection updater;

        private int   status;
        private float endTime;
        private float waitTime;

        private void Awake()
        {
            enabled  = false;
            updater  = (IUpdateBySection)this;
        }
        public void Play()
        {
            waitTime = 1.5f;
            status = 0;
            updater.MoveNext();
        }
        void IUpdateBySection.MoveNext()
        {
            switch (status)
            {
                case 0:
                    logoFaded = transform.GetComponent<Image>();
                    logoFaded.color = Color.black;
                    updateFunc = FadeIn;
                    enabled = true;
                    break;
                case 1:
                    endTime = Time.time + waitTime;
                    updateFunc = Wait;
                    break;
                case 2:
                    logoFaded.color = Color.white;
                    updateFunc = FadeOut;
                    break;
                case 3:
                    enabled = false;
                    gameObject.SetActive(false);
                    updateFunc = null;
                    OnOpening.instance.MoveNext();
                    break;
            }
            status += 1;
        }

        private void FadeIn()
        {
            float cValue = logoFaded.color.r + Time.deltaTime * Value.FADE_SPEED;
            logoFaded.color = new Color(cValue, cValue, cValue, 1f);
            if (cValue >= 1)
            {
                updater.MoveNext();
            }
        }
        private void Wait()
        {
            if (endTime < Time.time)
            {
                updater.MoveNext();
            }
        }
        private void FadeOut()
        {
            float cValue = logoFaded.color.r - Time.deltaTime * Value.FADE_SPEED;
            logoFaded.color = new Color(cValue, cValue, cValue, 1f);
            if (cValue <= 0)
            {
                updater.MoveNext();
            }
        }

        private void Update()
        {
            updateFunc();
        }
    }
    private class OpeningDemo : MonoBehaviour, IUpdateBySection
    {
        private IUpdateBySection.UpdateDele updater;

        private void Awake()
        {
            enabled = false;
        }
        public void Play()
        {
            Debug.Log("Not yet Play Demo => OnOpening.MoveNext();");
            gameObject.SetActive(false);
            OnOpening.instance.MoveNext();
        }
        public void MoveNext()
        {

        }
    }

    private static OnOpening instance;

    private OpeningLogo  logo;
    private OpeningDemo  demo;

    private int current = 0;

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
                Debug.Log("Call UITitle");
                //gameObject.SetActive(false);
                GameObject.Destroy(this.gameObject);
                break;
        }
    }
    private void OnDestroy()
    {
        AssetManager.ReleaseAsset(gameObject.GetInstanceID());
    }
}