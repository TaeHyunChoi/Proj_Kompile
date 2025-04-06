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
            MoveNext();
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
                                MoveNext();
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
                            MoveNext();
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
                    IngameManager.RemoveIngameHandler(IngameHandlerType.OPENING);
                    state = State.LOAD_PLAYER;
                    break;
                case State.LOAD_PLAYER:
                    Debug.Log(state);
                    state = State.INIT_FIELD;
                    break;
                case State.INIT_FIELD:
                    Debug.Log(state);
                    state = State.LOADING_FADEOUT;
                    loadingCurtainObject.On(false);
                    break;
                case State.LOADING_FADEOUT:
                    // Receive()
                    break;
                case State.CLOSE:
                    IngameManager.RemoveIngameHandler(this.handlerType);
                    return IngameHandlerState.SUCCESS;
                default:
                    return IngameHandlerState.FAILURE;
            }
            return IngameHandlerState.RUNNING;
        }

        public override void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}
