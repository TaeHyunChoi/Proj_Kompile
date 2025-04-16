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
            LOAD_DATA,
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

        public bool Receive_IngameEvent<T>(IngameEventType type, T data) where T : struct
        {
            if (type == IngameEventType.GET_ASSET)
            {
                if (data is OnGetAsset_GameObject onGetAsset)
                {
                    switch (onGetAsset.AssetCode)
                    {
                        case AssetCode.UI_LoadingCurtain:
                            if (true == AssetManager.TryGetIngameAsset(onGetAsset.InstanceID, out loadingCurtainObject))
                            {
                                loadingCurtainObject.On(true);
                                state = State.LOADING_FADEIN;
                                //IngameManager.MoveNextHandler(handlerType);
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
            else if (type == IngameEventType.END_OBJECT_PROCESS)
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
                            //IngameManager.MoveNextHandler(handlerType);
                            return true;
                    }
                }
            }

            return false;
        }

        //public override IngameHandlerState MoveNext()
        //{
        //    switch (state)
        //    {
        //        case State.NONE:
        //            Transform parent = AssetManager.GetCanvas(CanvasType.OVERLAY_LOADING).transform;
        //            loadTask = AssetManager.InstantiateGameObjectAssetAsync(AssetCode.UI_LoadingCurtain, parent, true);
        //            goto case State.LOAD_DATA;
        //        case State.LOAD_DATA:
        //            // Player.cs 가 필요하겠군?
        //            // 여기서 FieldHandler가 필요한 것인디.. 이것도 또 구조적인 문제에 부딪히는구만...
        //            // MapData도 먼저 가져올 순 있겠다. (생성은 로딩 후에 해도 될 것 같고)
        //            // 필요하면 로딩 커튼에 애니메이션 추가하던가...
        //            state = State.LOADING_FADEIN;
        //            break;
        //        case State.LOADING_FADEIN:
        //            // Receive() => MoveNext
        //            break;
        //        case State.INIT_FIELD:
        //            Debug.Log(state = State.INIT_FIELD);
        //            loadingCurtainObject.On(false);
        //            state = State.LOADING_FADEOUT;
        //            goto case State.LOADING_FADEOUT;
        //        case State.LOADING_FADEOUT:
        //            // Receive()
        //            break;
        //        case State.CLOSE:
        //            return IngameHandlerState.SUCCESS;
        //        default:
        //            return IngameHandlerState.FAILURE;
        //    }
        //    return IngameHandlerState.RUNNING;
        //}

        public override void Dispose()
        {
        }

        public override void Receive_Input(IDxInput.InputFlag inputFlag)
        {
            // nothing;
        }
    }
}
