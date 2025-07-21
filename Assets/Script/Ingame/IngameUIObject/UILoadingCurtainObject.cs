using Script.Index;
using Script.Interface;
using Script.Manager;
using Script.IngameMessage;
using UnityEngine;
using UnityEngine.UI;

public class UILoadingCurtainObject : IngameMonoBehaviourBase, IIngameUpdater
{
    private Image image;
    
    private State state;
    private float delta;
    private float alpha;

    private void Awake()
    {
        image = transform.GetComponent<Image>();
    }
    public void On(bool on)
    {
        state = State.FADE;

        if (true == on)
        {
            alpha = 0f;
            delta = 1f;
        }
        else
        {
            alpha = 1f;
            delta = -1.5f;
        }
    }
    public IngameUpdateState UpdateState()
    {
        switch (state)
        {
            case State.FADE:
                alpha += delta * Time.deltaTime;
                image.color = new Color(0f, 0f, 0f, alpha);

                var prev_alpha = alpha;
                alpha = System.Math.Clamp(alpha, 0, 1);
                Debug.Log($"[TEST] {prev_alpha:F6} => {alpha:F6}");
                if (alpha <= 0 || alpha >= 1)
                {
                    IngameManager.MoveNextEventType();
                    state = State.CLOSE;
                    Debug.Log($"[TEST] Close Loading ({alpha:F6})");
                }
                break;
            case State.CLOSE:
                return IngameUpdateState.SUCCESS;
        }

        return IngameUpdateState.RUNNING;
    }

    private enum State
    {
        FADE,
        CLOSE
    }
}
