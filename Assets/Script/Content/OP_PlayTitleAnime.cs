
namespace Script.Content
{
    using System.Threading.Tasks;
    using UnityEngine;
    using Script.Index;
    using static Script.Index.IDxInput;
    using Script.Manager;
    using Script.Interface;

    public class OP_PlayTitleAnime : ITaskUpdater, ITaskInput
    {
        private enum State
        { 
            NONE = 0,
            INSTANTIATE_OPENING_PREFAB,
            WAIT_INSTNATIATE_OPENTING_PREFAB,
            PLAY_COMPANY_LOGO,
            //PLAY_DEMO_PLAY,
            PLAY_TITLE_LOGO,
            END
        }

        //private readonly float alphaDelta = 0.75f;

        private Task<GameObject> loadAssetTask;
        private OP_TitleObject title;

        private State state;

        public OP_PlayTitleAnime()
        {
            state = State.NONE;
        }
        public IETaskState MoveNext()
        {
            switch (state)
            {
                case State.NONE:
                    ++state;
                    goto case State.INSTANTIATE_OPENING_PREFAB;

                case State.INSTANTIATE_OPENING_PREFAB:
                    Transform parent = AssetManager.GetCanvas(CanvasType.OVERLAY).transform;
                    loadAssetTask = AssetManager.GetGameObjectAssetAsync(EAssetName.OpeningGame, parent, true);
                    ++state;
                    break;

                case State.WAIT_INSTNATIATE_OPENTING_PREFAB:
                    if (loadAssetTask.IsCompletedSuccessfully)
                    {
                        title = (loadAssetTask.Result).GetComponent<OP_TitleObject>();
                        IngameManager.SetInputTarget(this);
                        ++state;
                    }
                    break;

                case State.PLAY_COMPANY_LOGO:
                    if (IETaskState.SUCCESS == title.MoveNext_PlayCompanyLogo())
                    {
                        ++state;
                    }
                    break;

                case State.PLAY_TITLE_LOGO:
                    if (IETaskState.SUCCESS == title.MoveNext_PlayTitleLogo())
                    {
                        ++state;
                    }
                    break;

                default:
                    return IETaskState.SUCCESS;
            }

            return IETaskState.RUNNING;
        }

        public void InputValue(EInputFlag inputFlag)
        {
            bool onAction = inputFlag.Contains(EInputFlag.ENTER | EInputFlag.ACTION);
            if (state == State.PLAY_COMPANY_LOGO
                && true == onAction)
            {
                //TODO: 데이터 테이블 로드 중이라면 확인 후 입력 막기
                title.EndPlayCompnayLogo();
            }
        }

        ~OP_PlayTitleAnime()
        {
            title = null;

            loadAssetTask.Dispose();
            loadAssetTask = null;
        }
    }
}

