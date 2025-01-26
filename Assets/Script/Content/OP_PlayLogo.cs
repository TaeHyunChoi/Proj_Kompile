
namespace Script.Content
{
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.UI;
    using Script.Index;
    using Script.Manager;

    public class OP_PlayLogo : ContentTaskBase
    {
        private enum State_PlayLogo
        { 
            NONE = 0,
            GET_OPENING_PREFAB = 1,
            WAIT_GET_OPENTING_PREFAB,
            LOGO_FADE_IN,
            WAIT_FADE,
            LOGO_FADE_OUT,
            END
        }

        private readonly float alphaDelta = 0.75f;

        private Task<GameObject> loadAssetTask;
        private Image logoImage;

        private State_PlayLogo state;
        private float waitTime;
        private float alpha;

        public OP_PlayLogo()
        {
            state = (State_PlayLogo)1;
            alpha = 0;
            waitTime = 0;
        }
        public override ContentTaskState MoveNext()
        {
            switch (state)
            {
                case State_PlayLogo.NONE:
#if UNITY_EDITOR || TEST_BUILD
                    OnlyDev.DevError.DebugWarning(ErrorCode.CANNOT_INIT_TASK_STATE, state.ToString());
#endif
                    ++state;
                    break;
                case State_PlayLogo.GET_OPENING_PREFAB:
                    Transform parent = AssetManager.GetCanvas(CanvasType.OVERLAY).transform;
                    loadAssetTask    = AssetManager.InstantiateGameObjectAsync(EAsset.OpeningGame.ToString(), parent, true);
                    ++state;
                    break;

                case State_PlayLogo.WAIT_GET_OPENTING_PREFAB:
                    if (loadAssetTask.IsCompletedSuccessfully)
                    {
                        logoImage = (loadAssetTask.Result).GetComponent<Image>();
                        ++state;
                    }
                    break;

                case State_PlayLogo.LOGO_FADE_IN :
                    alpha += Time.deltaTime * alphaDelta;
                    logoImage.color = new Color(1f, 1f, 1f, alpha);

                    if (1 <= alpha)
                    {
                        ++state;
                    }
                    break;

                case State_PlayLogo.WAIT_FADE:
                    if (waitTime < 1f)
                    {
                        waitTime += Time.deltaTime;
                    }
                    else
                    {
                        ++state;
                    }
                    break;

                case State_PlayLogo.LOGO_FADE_OUT :
                    alpha -= Time.deltaTime * (alphaDelta * 3);
                    logoImage.color = new Color(1f, 1f, 1f, alpha);

                    if (0 >= alpha)
                    {
                        ++state;
                    }
                    break;
                default:
                    return ContentTaskState.SUCCESS;
            }

            return ContentTaskState.RUNNING;
        }
        ~OP_PlayLogo()
        {
            loadAssetTask.Dispose();
        }
    }
}

