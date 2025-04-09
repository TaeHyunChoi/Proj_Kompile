namespace Script.Content
{
    using System.Threading.Tasks;
    using Script.Manager;
    using Script.Index;
    using Script.Interface;
    using Script.IngameMessage;
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

            WAIT,
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
        }

        public override IngameHandlerState MoveNext()
        {
            switch (state)
            {
                case State.NONE:
                case State.INIT_OPENING:
                    Transform parent = AssetManager.GetCanvas(CanvasType.OVERLAY).transform;
                    loadTask = AssetManager.InstantiateGameObjectAssetAsync(AssetCode.OP_TitleObject, parent, true);
                    break;
                case State.PLAY_OPENING:
                    // Receive() => next state;
                    // Invoke Input();
                    break;
                case State.LOAD_TITLE_MENU:
                    parent = AssetManager.GetCanvas(CanvasType.OVERLAY).transform;
                    loadTask = AssetManager.InstantiateGameObjectAssetAsync(AssetCode.UI_TitleMenuObject, parent, true);
                    //  Receive() => next state;
                    break;
                case State.SELECT_TITLE_MENU:
                    // Receive() => next state;
                    // Invoke Input();
                    break;
                case State.WAIT:
                    // MoveNext()로 호출하면 곧장 END로 빠져서 Hnadler가 날아감.
                    goto case State.END;
                case State.END:
                    return IngameHandlerState.SUCCESS;
                default:
                    Debug.Assert(false, $"OpeningHandler: Wrong State ({state}");
                    return IngameHandlerState.FAILURE;
            }

            return IngameHandlerState.RUNNING;
        }


        public bool Receive<T>(IngameMessageType type, T data) where T : struct
        {
            State nextState = State.NONE;

            switch (type)
            {
                case IngameMessageType.GET_ASSET:
                     if(data is OnGetAsset_GameObject onGetAsset)
                    {
                        switch (onGetAsset.AssetCode)
                        {
                            case AssetCode.OP_TitleObject:
                                if (true == AssetManager.TryGetGameObjectAsset(onGetAsset.InstanceID, out titleObject))
                                {
                                    nextState = State.PLAY_OPENING;
                                }
                                break;
                            case AssetCode.UI_TitleMenuObject:
                                if (true == AssetManager.TryGetGameObjectAsset(onGetAsset.InstanceID, out uiTitleMenu))
                                {
                                    nextState = State.SELECT_TITLE_MENU;
                                }
                                break;
                            default:
                                break;
                        }

                        if(null != loadTask)
                        {
                            loadTask.ContinueWith(task => task.Dispose());
                        }
                    }
                    break;
                case IngameMessageType.END_OBJECT_PROCESS:
                    if(data is OnEndProcess onEndProcess)
                    {
                        switch (onEndProcess.AssetCode)
                        {
                            case AssetCode.OP_TitleObject:      nextState = State.LOAD_TITLE_MENU; break;
                            case AssetCode.UI_TitleMenuObject:  nextState = State.END;  break;
                            default:
                                return false;
                        }
                    }
                    break;
                case IngameMessageType.SELECT_ITEM:
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
            IngameManager.MoveNextHandler(handlerType);
            return true;
        }
        private void SelectMenu(UITitleMenuObject.MenuType type)
        {
            switch (type)
            {
                case NEW_GAME:
                    IngameManager.AddIngameHander(IngameHandlerType.NEW_GAME);
                    uiTitleMenu.WaitUpdate();
                    state = State.WAIT;
                    // Receive => EndProcess
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
            AssetManager.Dispose(titleObject.gameObject.GetInstanceID());
            titleObject = null;

            AssetManager.Dispose(uiTitleMenu.gameObject.GetInstanceID());
            uiTitleMenu = null;

            MessageManager.Dispose(this);
        }

        public override void ReceiveInput(IDxInput.InputFlag inputFlag)
        {
            switch (state)
            {
                case State.PLAY_OPENING:        titleObject.Input(inputFlag); break;
                case State.SELECT_TITLE_MENU:   uiTitleMenu.Input(inputFlag); break;
                default:
                    break;
            }
        }
    }
}
