using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class OnOpening : MonoBehaviour
{
    private class OpeningLogo : MonoBehaviour, ISequenceUpdate
    {
        private Image   logo;
        private ISequenceUpdate.SequenceDele sequence;
        private int     status;
        private float   endTime;
        private float   waitTime;

        private void Awake()
        {
            enabled = false;

            status = 0;
            waitTime = 1.5f;
            logo = transform.GetComponent<Image>();
            logo.color = Color.black;
        }
        public void GotoNext()
        {
            switch (status++)
            {
                case 0:
                    sequence = FadeIn;

                    gameObject.SetActive(true);
                    enabled = true;
                    break;
                case 1:
                    endTime = Time.time + waitTime;
                    sequence = Wait;
                    break;
                case 2:
                    logo.color = Color.white;
                    sequence = FadeOut;
                    break;
                case 3:
                    enabled = false;
                    gameObject.SetActive(false);

                    opening.NextSequence();

                    logo = null;
                    sequence = null;
                    break;
            }
        }

        private void FadeIn()
        {
            float cValue = logo.color.r + Time.deltaTime * Value.FADE_SPEED;
            logo.color = new Color(cValue, cValue, cValue, 1f);
            if (cValue >= 1)
            {
                GotoNext();
            }
        }
        private void Wait()
        {
            if (endTime < Time.time)
            {
                GotoNext();
            }
        }
        private void FadeOut()
        {
            float cValue = logo.color.r - Time.deltaTime * Value.FADE_SPEED;
            logo.color = new Color(cValue, cValue, cValue, 1f);
            if (cValue <= 0)
            {
                GotoNext();
            }
        }

        private void Update()
        {
            sequence();
        }
    }
    private class OpeningDemo : MonoBehaviour, ISequenceUpdate
    {
        private int status;
        private void Awake()
        {
            enabled = false;
            gameObject.SetActive(false);

            status = 0;
        }
        public void GotoNext()
        {
            switch (status++)
            {
                case 0:
                    Debug.Log("Not yet Play Demo => OnOpening.MoveNext();");
                    opening.NextSequence();
                    break;
            }
        }
    }
    private class OpeningTitle : MonoBehaviour, ISequenceUpdate
    {
        private Image[] titleLogos;
        private ISequenceUpdate.SequenceDele sequence;
        private int status;

        private void Awake()
        {
            enabled = false;
            gameObject.SetActive(false);
            status = 0;
            titleLogos = new Image[2];
            titleLogos[0] = transform.GetChild(0).GetComponent<Image>();
            titleLogos[1] = transform.GetChild(1).GetComponent<Image>();
        }

        public void GotoNext()
        {
            switch (status)
            {
                case 0:
                    //타이틀 로고 : 위아래 자리 세팅
                    enabled = true;
                    gameObject.SetActive(true);

                    //dele: 지정 좌표까지 이동
                    sequence = MoveTitleLogo;
                    break;
                case 1:
                    //번쩍 => 타이틀 진짜 로고
                    //번쩍 제일 밝아지면 UITitle 띄우자. => opening.MoveNext() 통해서 호출
                    opening.NextSequence();
                    break;
            }
        }

        private void Update()
        {
            sequence();
        }

        private void MoveTitleLogo()
        {

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
        title = transform.GetChild(2).AddComponent<OpeningTitle>();
    }
    private void Start()
    {
        NextSequence();
    }
    private void NextSequence()
    {
        //타고 타고 들어 가는 게 너무 많은 것 같다.
        //코드 정리가 필요하닷..; 뭔가 진도가 되게 안나가네 흠...
        switch (current++)
        {
            case 0: 
                logo.GotoNext(); 
                break;
            case 1:
                logo = null;
                demo.GotoNext();
                break;
            case 2:
                demo = null;
                title.GotoNext();
                break;
            case 3:
                title = null;
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