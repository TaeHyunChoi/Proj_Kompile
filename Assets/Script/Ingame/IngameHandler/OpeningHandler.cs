namespace Script.Content
{
    using System.Threading.Tasks;
    using Script.Manager;
    using Script.Index;
    using UnityEngine;
    using static UITitleMenuObject.MenuType;

    public partial class OpeningHandler : _IngameHandlerBase, IMessageReceiver
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

        private State state;
        private Task<GameObject>    loadTask;
        private TitleObject         titleObject;
        private UITitleMenuObject   uiTitleMenu;

        public OpeningHandler()
        {
            handlerType = IngameHandlerType.OPENING;

            state       = State.NONE;
            MessageManager.AddReceiver(this, hasInput: true);

            MoveNext();
        }

        public bool Receive<T>(MessageType type, T data) where T : struct
        {
            State nextState = State.NONE;

            switch (type)
            {
                case MessageType.GET_ASSET:
                     if(data is OnGetAsset_GameObject onGetAsset)
                    {
                        switch (onGetAsset.AssetCode)
                        {
                            case AssetCode.OP_TitleObject:
                                if (false == AssetManager.TryGetGameObjectAsset(onGetAsset.InstanceID, out titleObject))
                                {
                                    // error
                                    goto default;
                                }
                                nextState = State.PLAY_OPENING;
                                break;
                            case AssetCode.UI_TitleMenuObject:
                                if (true == AssetManager.TryGetGameObjectAsset(onGetAsset.InstanceID, out uiTitleMenu))
                                {
                                    // error
                                    goto default;
                                }
                                nextState = State.SELECT_MENU;
                                break;
                            default:
                                // error
                                return false;
                        }

                        loadTask.Dispose();
                    }
                    break;
                case MessageType.END_OBJECT_PROCESS:
                    if(data is OnEndProcess onEndProcess)
                    {
                        switch (onEndProcess.AssetCode)
                        {
                            case AssetCode.OP_TitleObject:      nextState = State.INSTANTIATE_UI_TITLE_MENU; break;
                            case AssetCode.UI_TitleMenuObject:  nextState = State.END;  break;
                            default:
                                // error
                                return false;
                        }
                    }
                    break;
                case MessageType.SELECT_ITEM:
                    if (data is OnSelectItem onSelectItem)
                    {
                        var menuType = (UITitleMenuObject.MenuType)onSelectItem.ValueInt;
                        SelectMenu(menuType);
                    }
                    break;
                case MessageType.INPUT_CONTROL:
                    if (data is OnInputControl onInputCtrl)
                    {
                        return InvokeInput(state, onInputCtrl.inputFlag);
                    }
                    return false;
                default:
                    Debug.Assert(true, $"OpeningHandler: Wrong Receive {type}");
                    return false;
            }

            if (State.NONE == nextState)
            {
                return false;
            }

            state = nextState;
            MoveNext();
            return true;
        }
        private void SelectMenu(UITitleMenuObject.MenuType type)
        {
            switch (type)
            {
                case NEW_GAME:
                    // IngameManager.NewGame(); 을 호출하면 아다리가 맞긴 함;
                    // 다른 핸들러까지 제어를 해야 하므로 IngameManager에게 결재 올린다.
                    break;
                case LOAD_GAME:
                    break;
                case OPTION:
                    break;
                case EXIT:
                    break;
                default:
                    // error? state 유지
                    return;
            }
        }
        private bool InvokeInput(State nowState, IDxInput.InputFlag inputFlag)
        {
            switch (nowState)
            {
                case State.INSTANTIATE_PRF_OPENING:
                case State.PLAY_OPENING: 
                    return titleObject.Input(inputFlag);

                case State.INSTANTIATE_UI_TITLE_MENU:
                case State.SELECT_MENU:  
                    return uiTitleMenu.Input(inputFlag);

                default:
                    break;
            }

            return false;
        }

        public override IngameHandlerState MoveNext()
        {
            Debug.Log($"[MoveNext] {state}");
            switch (state)
            {
                case State.NONE:
                    state = State.INSTANTIATE_PRF_OPENING;
                    goto case State.INSTANTIATE_PRF_OPENING;

                case State.INSTANTIATE_PRF_OPENING:
                    Transform parent = AssetManager.GetCanvas(CanvasType.OVERLAY).transform;
                    loadTask = AssetManager.InstantiateGameObjectAssetAsync(AssetCode.OP_TitleObject, parent, true);
                    //  Receive() => next state;
                    break;
                case State.PLAY_OPENING:
                    //  Receive() => next state;
                    break;

                case State.INSTANTIATE_UI_TITLE_MENU:
                    parent = AssetManager.GetCanvas(CanvasType.OVERLAY).transform;
                    loadTask = AssetManager.InstantiateGameObjectAssetAsync(AssetCode.UI_TitleMenuObject, parent, true);
                    //  Receive() => next state;
                    break;
                case State.SELECT_MENU:
                    //  Receive() => next state;
                    break;

                case State.END:
                    return IngameHandlerState.SUCCESS;

                default:
                    return IngameHandlerState.FAILURE;
            }

            return IngameHandlerState.RUNNING;
        }

        public override void Dispose()
        {
            AssetManager.Dispose(titleObject.GetInstanceID());
            titleObject = null;

            AssetManager.Dispose(uiTitleMenu.GetInstanceID());
            uiTitleMenu = null;

            MessageManager.Dispose(this);
        }
    }
}
