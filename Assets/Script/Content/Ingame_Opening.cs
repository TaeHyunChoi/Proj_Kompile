namespace Script.Content
{
    using System.Threading.Tasks;
    using Script.Manager;
    using Script.Index;
    using UnityEngine;

    public partial class Ingame_Opening
    {
        private enum State
        {
            NONE = 0,
            
            INSTANTIATE_PRF_OPENING,
            PLAY_OPENING,

            INSTANTIATE_UI_TITLE_MENU,
            SELECT_MENU,

            END
        }
    }
    
    public partial class Ingame_Opening : IngameLogicBase, IMessageReceiver
    {
        private State state;
        private Task<GameObject> loadTask;


        public Ingame_Opening()
        {
            state = State.NONE;
            ingameLogicType = IngameLogicIndex.OPENING;

            MessageManager.AddReceiver(this);

            MoveNext();
        }

        public void Receive<T>(MessageType type, T data) where T : struct
        {
            State nextState = State.NONE;

            if (MessageType.GET_ASSET == type
                && data is OnGetAsset_GameObject onGetAsset)
            {
                AssetCode code = onGetAsset.AssetCode;

                if (AssetCode.OP_TitleObject == code)
                {
                    nextState = State.PLAY_OPENING;
                    loadTask.Dispose();
                }
                else if (AssetCode.UI_TitleMenuObject == code)
                {
                    nextState = State.SELECT_MENU;
                    loadTask.Dispose();
                }
            }

            else if (MessageType.END_OBJECT_PROCESS == type
                && data is OnEndProcess onEndProcess)
            {
                AssetCode assetIndex = onEndProcess.AssetCode;

                if (AssetCode.OP_TitleObject == assetIndex)
                {
                    nextState = State.INSTANTIATE_UI_TITLE_MENU;
                }
                else if (AssetCode.UI_TitleMenuObject == assetIndex)
                {
                    nextState = State.END;
                }
            }

            if (State.NONE != nextState)
            {
                state = nextState;
                MoveNext();
            }
        }

        public override IngameState MoveNext()
        {
            switch (state)
            {
                case State.NONE:
                    state = State.INSTANTIATE_PRF_OPENING;
                    goto case State.INSTANTIATE_PRF_OPENING;

                case State.INSTANTIATE_PRF_OPENING:
                    Transform parent = AssetManager.GetCanvas(CanvasType.OVERLAY).transform;
                    loadTask = AssetManager.GetGameObjectAssetAsync(AssetCode.OP_TitleObject, parent, true);
                    //  Receive() => next state;
                    break;
                case State.PLAY_OPENING:
                    //  Receive() => next state;
                    break;

                case State.INSTANTIATE_UI_TITLE_MENU:
                    parent = AssetManager.GetCanvas(CanvasType.OVERLAY).transform;
                    loadTask = AssetManager.GetGameObjectAssetAsync(AssetCode.UI_TitleMenuObject, parent, true);
                    //  Receive() => next state;
                    break;
                case State.SELECT_MENU:
                    //  Receive() => next state;
                    break;

                case State.END:
                    MessageManager.Dispose(this);
                    return IngameState.SUCCESS;

                default:
                    return IngameState.FAILURE;
            }

            return IngameState.RUNNING;
        }
    }
}
