using Script.Index;
using Script.Interface;
using Script.Manager;
using Script.IngameMessage;
using UnityEngine;
using UnityEngine.UI;

public class UILoadingCurtainObject : MonoBehaviour, IIngameUpdater
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

    private enum State
    {
        NONE = 0,
        FADE,
    }

    private float sign;

    private State state;
    private float alpha;

    public void On(bool on)
    {
        state = State.NONE;
        if (true == on)
        {
            alpha = 0f;
            sign = 1f;
        }
        else
        {
            alpha = 1f;
            sign = -1f;
        }
        IngameManager.AddUpdater(UpdaterType.UPDATE, this);
    }
    public IngameUpdateState UpdateState()
    {
        switch (state)
        {
            case State.NONE:
                //loadingCurtain = AssetManager.GetLoadingCurtain();
                ++state;
                goto case State.FADE;

            case State.FADE:
                alpha += sign * Time.deltaTime;
                FadeAlpha(alpha);
                //loadingCurtain.FadeAlpha(alpha);

                alpha = System.Math.Clamp(alpha, 0, 1);
                if (alpha <= 0 || alpha >= 1)
                {
                    MessageManager.Publish(IngameMessageType.END_OBJECT_PROCESS, new OnEndProcess(AssetCode.UI_LoadingCurtain, end: (int)alpha));
                    return IngameUpdateState.SUCCESS;
                }
                break;

        }

        return IngameUpdateState.RUNNING;
    }
}
