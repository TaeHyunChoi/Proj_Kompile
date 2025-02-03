
namespace Script.Content
{
    using System.Threading.Tasks;
    using UnityEngine;
    using Script.Index;
    using static Script.Index.IDxInput;
    using Script.Manager;
    using Script.Interface;

    public class OP_PlayTitleAnime : ITaskUpdater
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

        private Task<GameObject> loadAssetTask;
        private OP_TitleObject title;

        private State state;
        private EInputFlag inputFlag => InputManager.GetInputFlag();

        public OP_PlayTitleAnime()
        {
            state = State.NONE;
        }
        public ETaskState MoveNext()
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
                        ++state;
                    }
                    break;

                case State.PLAY_COMPANY_LOGO:
                    if (ETaskState.SUCCESS == title.MoveNext_PlayCompanyLogo())
                    {
                        ++state;
                    }
                    if (true == inputFlag.Contains(EInputFlag.ENTER | EInputFlag.ACTION))
                    {
                        //TODO: 데이터 테이블 로드 중이라면 확인 후 입력 막기
                        title.EndPlayCompnayLogo();
                    }
                    break;

                case State.PLAY_TITLE_LOGO:
                    if (ETaskState.SUCCESS == title.MoveNext_PlayTitleLogo())
                    {
                        ++state;
                    }
                    break;

                default:
                    return ETaskState.SUCCESS;
            }

            return ETaskState.RUNNING;
        }

        ~OP_PlayTitleAnime()
        {
            title = null;

            loadAssetTask.Dispose();
            loadAssetTask = null;
        }
    }
}

