using Script.Index;
using Script.Interface;
using Script.Manager;
using System.Threading.Tasks;
using UnityEngine;

public class UI_TitleMenu : IContentTaskUpdater
{
    private enum State
    {
        NONE = 0,

        INSTANTIATE_UI_PREFAB,
        WAIT_INSTANTIATE_UI_PREFAB,
        PLAY_UPDATE,
        END
    }

    private Task<GameObject> loadAssetTask;
    private UI_TitleMenuObject titleMenu;
    private State state;

    public UI_TitleMenu()
    {
        state = State.NONE;
    }

    public ContentTaskState MoveNext()
    {
        switch (state)
        {
            case State.NONE:
                ++state;
                goto case State.INSTANTIATE_UI_PREFAB;

            // 여기 부분을 한 데 묶어서 처리할 수도 있겠음.
            // (코루틴 안의 코루틴 느낌으로..)
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

            case State.PLAY_UPDATE:
                // input을 받아서 여차저차 해야겠습니다?..
                break;
            default:
                return ContentTaskState.SUCCESS;
        }

        return ContentTaskState.RUNNING;
    }

    ~UI_TitleMenu()
    {
        loadAssetTask.Dispose();
        loadAssetTask = null;
    }
}
