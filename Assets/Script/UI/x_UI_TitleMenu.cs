using System.Threading.Tasks;
using UnityEngine;
using Script.Index;
using static Script.Index.IDxInput;
using Script.Interface;
using Script.Manager;


public class x_UI_TitleMenu /*: IIngameUpdater*/
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

    //public UI_TitleMenu()
    //{
    //    state = State.NONE;
    //}

    //public UpdaterState UpdateState()
    //{
    //    switch (state)
    //    {
    //        case State.NONE:
    //            ++state;
    //            goto case State.INSTANTIATE_UI_PREFAB;

    //        // 여기 부분을 한 데 묶어서 처리할 수도 있겠음. (코루틴 안의 코루틴 느낌으로..)
    //        case State.INSTANTIATE_UI_PREFAB:
    //            Transform parent = AssetManager.GetCanvas(CanvasType.OVERLAY).transform;
    //            loadAssetTask = AssetManager.GetGameObjectAssetAsync(AssetIndex.UI_TitleMenuObject, parent, true);
    //            ++state;
    //            break;
    //        case State.WAIT_INSTANTIATE_UI_PREFAB:
    //            if (loadAssetTask.IsCompletedSuccessfully)
    //            {
    //                titleMenu = (loadAssetTask.Result).GetComponent<UI_TitleMenuObject>();
    //                ++state;
    //            }
    //            break;

    //        case State.UPDATE:

    //            //// move
    //            //if (true == InputFlag.Contains(EInputFlag.UP | EInputFlag.DOWN))
    //            //{
    //            //    titleMenu.OnSelect_Move(InputFlag);
    //            //}

    //            //// action
    //            //if (true == InputFlag.Contains(EInputFlag.ENTER | EInputFlag.ACTION))
    //            //{
    //            //    if (0 == titleMenu.OnSelect_Enter())
    //            //    {
    //            //        state = State.END;
    //            //        goto case default;
    //            //    }
    //            //    else
    //            //    {
    //            //        state = State.WAIT;
    //            //    }
    //            //}

    //            // object update

    //            break;

    //        case State.WAIT:
    //            // 다른 조작이 들어오기 전까지 대기
    //            break;

    //        default:
    //            InputManager.Clear();
    //            return UpdaterState.SUCCESS;
    //    }

    //    return UpdaterState.RUNNING;
    //}

    //~UI_TitleMenu()
    //{
    //    loadAssetTask.Dispose();
    //    loadAssetTask = null;

    //    titleMenu = null;
    //}
}
