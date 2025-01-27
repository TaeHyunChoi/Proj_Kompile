
namespace Script.Content
{
    using System.Threading.Tasks;
    using UnityEngine;
    using Script.Index;
    using Script.Manager;

    public class OP_PlayTitleAnime : ContentTaskBase
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

        private readonly float alphaDelta = 0.75f;

        private Task<GameObject> loadAssetTask;
        private OP_TitleObject title;

        private State state;

        public OP_PlayTitleAnime()
        {
            state = State.NONE;
        }
        public override ContentTaskState MoveNext()
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
                    if (ContentTaskState.SUCCESS == title.MoveNext_PlayCompanyLogo(inputMask))
                    {
                        ++state;
                    }
                    break;

                case State.PLAY_TITLE_LOGO:
                    if (ContentTaskState.SUCCESS == title.MoveNext_PlayTitleLogo(inputMask))
                    {
                        ++state;
                    }
                    break;

                default:
                    return ContentTaskState.SUCCESS;
            }

            return ContentTaskState.RUNNING;
        }
        ~OP_PlayTitleAnime()
        {
            title = null;

            loadAssetTask.Dispose();
            loadAssetTask = null;
        }
    }
}

