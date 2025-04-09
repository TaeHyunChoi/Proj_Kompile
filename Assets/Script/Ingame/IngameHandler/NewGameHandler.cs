namespace Script.Content
{
    using Script.Data;
    using Script.Index;
    using Script.Interface;
    using Script.IngameMessage;
    using Script.Manager;
    using System.Threading.Tasks;
    using UnityEngine;

    public class NewGameHandler : _IngameHandlerBase, IMessageReceiver
    {
        private enum State
        { 
            NONE = 0,
            LOADING_FADEIN,
            END_OPENING,
            LOAD_PLAYER,
            INIT_FIELD, // + LOAD_MAP
            LOADING_FADEOUT,
            CLOSE
        }

        private Task<GameObject> loadTask;
        private Task loadMapTask;
        private UILoadingCurtainObject loadingCurtainObject;
        private State state;

        public NewGameHandler()
        {
            handlerType = IngameHandlerType.NEW_GAME;
            MessageManager.AddReceiver(this, false);
            state = State.NONE;
        }

        public bool Receive<T>(IngameMessageType type, T data) where T : struct
        {
            if (type == IngameMessageType.GET_ASSET)
            {
                if (data is OnGetAsset_GameObject onGetAsset)
                {
                    switch (onGetAsset.AssetCode)
                    {
                        case AssetCode.UI_LoadingCurtain:
                            if (true == AssetManager.TryGetGameObjectAsset(onGetAsset.InstanceID, out loadingCurtainObject))
                            {
                                loadingCurtainObject.On(true);
                                state = State.LOADING_FADEIN;
                                IngameManager.MoveNextHandler(handlerType);
                                return true;
                            }
                            break;
                    }
                }
                else if (data is OnGetAsset_MapGridData getRawMapGridData)
                {
                    AssetCode code = getRawMapGridData.AssetCode;
                    MapGridData grid = getRawMapGridData.Data;

                    loadMapTask.Dispose();
                    loadMapTask = null;

                    state = State.CLOSE;

                    return true;
                }
            }
            else if (type == IngameMessageType.END_OBJECT_PROCESS)
            {
                if (data is OnEndProcess onEnd)
                {
                    switch (onEnd.AssetCode)
                    {
                        case AssetCode.UI_LoadingCurtain:
                            if (1 == onEnd.endCode)
                            {
                                state = State.END_OPENING;
                            }
                            else
                            {
                                state = State.CLOSE;
                            }
                            IngameManager.MoveNextHandler(handlerType);
                            return true;
                    }
                }
            }

            return false;
        }

        public override IngameHandlerState MoveNext()
        {
            switch (state)
            {
                case State.NONE:
                    Transform parent = AssetManager.GetCanvas(CanvasType.OVERLAY_LOADING).transform;
                    loadTask = AssetManager.InstantiateGameObjectAssetAsync(AssetCode.UI_LoadingCurtain, parent, true);
                    state = State.LOADING_FADEIN;
                    break;
                case State.LOADING_FADEIN:
                    // Receive()
                    break;
                case State.END_OPENING:
                    Debug.Log(state);
                    goto case State.LOAD_PLAYER;
                case State.LOAD_PLAYER:
                    Debug.Log(state = State.LOAD_PLAYER);
                    goto case State.INIT_FIELD;
                case State.INIT_FIELD:
                    Debug.Log(state = State.INIT_FIELD);
                    loadingCurtainObject.On(false);
                    state = State.LOADING_FADEOUT;
                    goto case State.LOADING_FADEOUT;
                case State.LOADING_FADEOUT:
                    // Receive()
                    break;
                case State.CLOSE:
                    return IngameHandlerState.SUCCESS;
                default:
                    return IngameHandlerState.FAILURE;
            }
            return IngameHandlerState.RUNNING;
        }

        public override void Dispose()
        {
        }

        public override void ReceiveInput(IDxInput.InputFlag inputFlag)
        {
            // nothing;
        }
    }
}
