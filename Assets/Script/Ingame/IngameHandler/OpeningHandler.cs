namespace Script.Content
{
    using Script.Manager;
    using Script.Index;
    using Script.Interface;
    using Script.IngameMessage;
    using static UITitleMenuObject.MenuType;
    using UnityEngine;

    public partial class OpeningHandler : _IngameHandlerBase, IMessageReceiver
    {
        private TitleObject         titleObject;
        private UITitleMenuObject   uiTitleMenuObject;
        private InputOpening        inputTarget;

        public OpeningHandler()
        {
            handlerType = IngameHandlerType.OPENING;
            inputTarget = InputOpening.NONE;

            MessageManager.AddReceiver(this, hasInput: true);

            ExecuteIngameEventAsync(IngameEventType.OPENING_INSTANTIATE_TITLE);
        }


        // ingame evnet
        public async void ExecuteIngameEventAsync(IngameEventType message_type)
        {
            try
            {
                switch (message_type)
                {
                    case IngameEventType.OPENING_INSTANTIATE_TITLE:
                        titleObject = await AssetManager.InstantiateGameObjectAsync<TitleObject>(AssetCode.OP_TitleObject, CanvasType.OVERLAY, true);
                        inputTarget = InputOpening.OPENING_OBJECT;
                        break;
                    case IngameEventType.OPENING_LOAD_TITLE_MENU:
                        uiTitleMenuObject = await AssetManager.InstantiateGameObjectAsync<UITitleMenuObject>(AssetCode.UI_TitleMenuObject, CanvasType.OVERLAY, true);
                        inputTarget = InputOpening.UI_TITLE_MENU_OBJECT;
                        break;
                    case IngameEventType.OPENING_SELECT_NEW_GAME:
                        IngameManager.AddIngameHander(IngameHandlerType.NEW_GAME);
                        uiTitleMenuObject.WaitUpdate();
                        inputTarget = InputOpening.NONE;
                        break;
                    default:
                        break;
                }
            }
            catch
            {
                inputTarget = InputOpening.NONE;
                Debug.Assert(false);
            }
        }
        public bool Receive_IngameEvent<T>(IngameEventType message_type, T data) where T : struct
        {
            if (data is OnEndProcess onEndProcess
                && AssetCode.OP_TitleObject == onEndProcess.AssetCode)
            {
                ExecuteIngameEventAsync(IngameEventType.OPENING_LOAD_TITLE_MENU);
                return true;
            }

            return false;
        }


        // input -> ingame event
        public override void Receive_Input(IDxInput.InputFlag inputFlag)
        {
            switch (inputTarget)
            {
                case InputOpening.OPENING_OBJECT: 
                    titleObject.Input(inputFlag); 
                    break;
                case InputOpening.UI_TITLE_MENU_OBJECT:
                    var menuIndex = (UITitleMenuObject.MenuType)uiTitleMenuObject.Input(inputFlag);
                    IngameEventType next_event_type;

                    switch (menuIndex)
                    {
                        case NEW_GAME:  next_event_type = IngameEventType.OPENING_SELECT_NEW_GAME;  break;
                        case LOAD_GAME: next_event_type = IngameEventType.OPENING_SELECT_LOAD_GAME; break;
                        case OPTION:    next_event_type = IngameEventType.OPENING_SELECT_OPTION;    break;
                        case EXIT:      next_event_type = IngameEventType.OPENING_SELECT_EXIT;      break;
                        default: return;
                    }

                    ExecuteIngameEventAsync(next_event_type);
                    break;
                default:
                    break;
            }
        }


        // dispose
        public override void Dispose()
        {
            AssetManager.Dispose(titleObject.gameObject.GetInstanceID());
            titleObject = null;

            AssetManager.Dispose(uiTitleMenuObject.gameObject.GetInstanceID());
            uiTitleMenuObject = null;

            MessageManager.Dispose(this);
        }


        // data type
        private enum InputOpening
        {
            NONE = 0,

            OPENING_OBJECT,
            UI_TITLE_MENU_OBJECT,
        }
    }
}
