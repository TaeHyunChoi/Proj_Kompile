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
            NONE,

            INIT_OPENING,
            PLAY_OPENING,

            LOAD_TITLE_MENU,
            SELECT_TITLE_MENU,

            END
        }

        private State state;
        private Task<GameObject>    loadTask;
        private TitleObject         titleObject;
        private UITitleMenuObject   uiTitleMenu;

        public OpeningHandler()
        {
            handlerType = IngameHandlerType.OPENING;
            state       = State.INIT_OPENING;
            MessageManager.AddReceiver(this, hasInput: true);

            MoveNext();
        }

        public override IngameHandlerState MoveNext()
        {
            switch (state)
            {
                case State.NONE:
                case State.INIT_OPENING:
                    Transform parent = AssetManager.GetCanvas(CanvasType.OVERLAY).transform;
                    loadTask = AssetManager.InstantiateGameObjectAssetAsync(AssetCode.OP_TitleObject, parent, true);
                    state = State.PLAY_OPENING;
                    break;
                case State.PLAY_OPENING:
                    // Receive() => next state;
                    // Invoke Input();
                    break;
                case State.LOAD_TITLE_MENU:
                    parent = AssetManager.GetCanvas(CanvasType.OVERLAY).transform;
                    loadTask = AssetManager.InstantiateGameObjectAssetAsync(AssetCode.UI_TitleMenuObject, parent, true);
                    state = State.SELECT_TITLE_MENU;
                    //  Receive() => next state;
                    break;
                case State.SELECT_TITLE_MENU:
                    // Receive() => next state;
                    // Invoke Input();
                    break;
                case State.END:
                    Dispose();
                    return IngameHandlerState.SUCCESS;
                default:
                    Debug.Assert(false, $"OpeningHandler: Wrong State ({state}");
                    return IngameHandlerState.FAILURE;
            }

            return IngameHandlerState.RUNNING;
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
                                if (true == AssetManager.TryGetGameObjectAsset(onGetAsset.InstanceID, out titleObject))
                                {
                                    nextState = state;
                                }
                                else
                                {
                                    Debug.Assert(false, $"OpeningHandler: Fail To Get Asset ({type}, {onGetAsset.AssetCode})");
                                }
                                break;
                            case AssetCode.UI_TitleMenuObject:
                                if (true == AssetManager.TryGetGameObjectAsset(onGetAsset.InstanceID, out uiTitleMenu))
                                {
                                    nextState = state;
                                }
                                else
                                {
                                    Debug.Assert(false, $"OpeningHandler: Fail To Get Asset ({type}, {onGetAsset.AssetCode})");
                                }
                                break;
                            default:
                                Debug.Assert(false, $"OpeningHandler: Wrong Asset Code ({type}, {onGetAsset.AssetCode})");
                                break;
                        }

                        loadTask.Dispose();
                    }
                    break;
                case MessageType.END_OBJECT_PROCESS:
                    if(data is OnEndProcess onEndProcess)
                    {
                        switch (onEndProcess.AssetCode)
                        {
                            case AssetCode.OP_TitleObject:      nextState = State.LOAD_TITLE_MENU; break;
                            case AssetCode.UI_TitleMenuObject:  nextState = State.END;  break;
                            default:
                                Debug.Assert(false, $"OpeningHandler: Wrong Asset Code ({type}, {onEndProcess.AssetCode})");
                                return false;
                        }
                    }
                    break;
                case MessageType.INPUT_CONTROL:
                    if (data is OnInputControl onInputCtrl)
                    {
                        return InvokeInput(state, onInputCtrl.inputFlag);
                    }
                    return false;
                case MessageType.SELECT_ITEM:
                    if (data is OnSelect_UITitleMenu onSelect)
                    {
                        var menuType = (UITitleMenuObject.MenuType)onSelect.ValueInt;
                        SelectMenu(menuType);
                        nextState = state;
                    }
                    break;
                default:
                    Debug.Assert(false, $"OpeningHandler: Wrong Type ({type}, -)");
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
        private bool InvokeInput(State nowState, IDxInput.InputFlag inputFlag)
        {
            switch (nowState)
            {
                case State.PLAY_OPENING: 
                    return titleObject.Input(inputFlag);

                case State.SELECT_TITLE_MENU:  
                    return uiTitleMenu.Input(inputFlag);

                default:
                    break;
            }

            return false;
        }
        private void SelectMenu(UITitleMenuObject.MenuType type)
        {
            switch (type)
            {
                case NEW_GAME:
                    // IngameManager.AddHandler(NewGameHandler);
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
