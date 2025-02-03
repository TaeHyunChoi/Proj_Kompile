using Script.Index;
using Script.Interface;
using Script.Manager;
using UnityEngine;

public class UI_LoadingCurtain : ITaskUpdater
{
    private enum State
    { 
        NONE = 0,
        FADE,
    }

    private readonly float sign;

    private UI_LoadingCurtainObject loadingCurtain;
    private State state;
    private float alpha;

    public UI_LoadingCurtain(bool on)
    {
        state = State.NONE;
        if (true == on)
        {
            alpha    =  0f;
            sign     =  1f;
        }
        else
        {
            alpha    =  1f;
            sign     = -1f;
        }
    }

    public ETaskState MoveNext()
    {
        switch(state)
        {
            case State.NONE:
                loadingCurtain = AssetManager.GetLoadingCurtain();
                ++state;
                goto case State.FADE;

            case State.FADE:
                alpha += sign * Time.deltaTime;
                loadingCurtain.FadeAlpha(alpha);

                alpha = System.Math.Clamp(alpha, 0, 1);
                if (alpha <= 0 || alpha >= 1)
                {
                    return ETaskState.SUCCESS;
                }
                break;

        }

        return ETaskState.RUNNING;
    }
}
