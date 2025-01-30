using System.Threading.Tasks;
using UnityEngine;
using Script.Index;
using static Script.Index.IDxInput;
using Script.Interface;
using Script.Manager;


public class UI_TitleMenu : ITaskUpdater, ITaskInput
{
    private enum State
    {
        NONE = 0,

        INSTANTIATE_UI_PREFAB,
        WAIT_INSTANTIATE_UI_PREFAB,
        UPDATE,
        END
    }

    private Task<GameObject> loadAssetTask;
    private UI_TitleMenuObject titleMenu;
    private State state;
    private EInputFlag inputFlag;

    public UI_TitleMenu()
    {
        state = State.NONE;
    }

    public IETaskState MoveNext()
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
                    IngameManager.SetInputTarget(this);
                    ++state;
                }
                break;

            case State.UPDATE:

                bool onMove = inputFlag.Contains(EInputFlag.UP | EInputFlag.UP_HOLD | EInputFlag.DOWN | EInputFlag.DOWN_HOLD);
                if (true == onMove
                    && false == titleMenu.OnMove(inputFlag))
                {
                    // error
                }

                // action
                bool onAction = inputFlag.Contains(EInputFlag.ENTER | EInputFlag.ACTION);
                if (true == onAction
                    && false == titleMenu.OnEnter(inputFlag))
                {
                    // error
                }


                break;
            default:
                return IETaskState.SUCCESS;
        }

        return IETaskState.RUNNING;
    }

    public void InputValue(EInputFlag inputFlag)
    {
        // MoveNext().UPDATE 에서 제어하므로 여기선 입력값만 바꿈.
        this.inputFlag = inputFlag;
    }

    ~UI_TitleMenu()
    {
        loadAssetTask.Dispose();
        loadAssetTask = null;

        titleMenu = null;
    }
}
