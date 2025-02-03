using System.Threading.Tasks;
using UnityEngine;
using Script.Index;
using static Script.Index.IDxInput;
using Script.Interface;
using Script.Manager;


public class UI_TitleMenu : ITaskUpdater
{
    private enum State
    {
        NONE = 0,

        INSTANTIATE_UI_PREFAB,
        WAIT_INSTANTIATE_UI_PREFAB,
        UPDATE,
        WAIT,
        END
    }

    private Task<GameObject> loadAssetTask;
    private UI_TitleMenuObject titleMenu;
    private State state;
    private EInputFlag InputFlag => InputManager.GetInputFlag();

    public UI_TitleMenu()
    {
        state = State.NONE;
    }

    public ETaskState MoveNext()
    {
        switch (state)
        {
            case State.NONE:
                ++state;
                goto case State.INSTANTIATE_UI_PREFAB;

            // 여기 부분을 한 데 묶어서 처리할 수도 있겠음. (코루틴 안의 코루틴 느낌으로..)
            case State.INSTANTIATE_UI_PREFAB:
                Transform parent = AssetManager.GetCanvas(CanvasType.OVERLAY).transform;
                loadAssetTask = AssetManager.GetGameObjectAssetAsync(EAssetName.UITitle, parent, true);
                ++state;
                break;
            case State.WAIT_INSTANTIATE_UI_PREFAB:
                if (loadAssetTask.IsCompletedSuccessfully)
                {
                    titleMenu = (loadAssetTask.Result).GetComponent<UI_TitleMenuObject>();
                    ++state;
                }
                break;

            case State.UPDATE:

                // move
                if (true == InputFlag.Contains(EInputFlag.UP | EInputFlag.DOWN))
                {
                    titleMenu.OnSelect_Move(InputFlag);
                }

                // action
                if (true == InputFlag.Contains(EInputFlag.ENTER | EInputFlag.ACTION))
                {
                    if (0 == titleMenu.OnSelect_Enter())
                    {
                        state = State.END;
                        goto case default;
                    }
                    else
                    {
                        state = State.WAIT;
                    }
                }

                // object update

                break;

            case State.WAIT:
                // 다른 조작이 들어오기 전까지 대기
                break;

            default:
                InputManager.Clear();
                return ETaskState.SUCCESS;
        }

        return ETaskState.RUNNING;
    }

    ~UI_TitleMenu()
    {
        loadAssetTask.Dispose();
        loadAssetTask = null;

        titleMenu = null;
    }
}
