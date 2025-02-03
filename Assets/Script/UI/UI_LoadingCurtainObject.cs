using UnityEngine;
using UnityEngine.UI;

public class UI_LoadingCurtainObject : MonoBehaviour
{
    private Image image;

    private void Awake()
    {
        image = transform.GetComponent<Image>();
    }
    public void FadeAlpha(float alpha)
    {
        image.color = new Color(0f, 0f, 0f, alpha);
    }
}
