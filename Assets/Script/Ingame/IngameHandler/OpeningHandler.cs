namespace Script.Content
{
    using Script.Manager;
    using Script.Index;
    using Script.Interface;
    using Script.IngameMessage;
    using static UITitleMenuObject.MenuType;

    public partial class OpeningHandler : _IngameHandlerBase, IMessageReceiver
    {
        private UITitleObject     titleObject;
        private UITitleMenuObject uiTitleMenuObject;
        private InputOpening      inputTarget;

        public OpeningHandler() : base()
        {
            handlerType = IngameHandlerType.OPENING;
            inputTarget = InputOpening.NONE;

            ExecuteIngameEventAsync(IngameEventType.OPENING_INSTANTIATE_TITLE);
        }


        // Ingame Handler
        protected override async void ExecuteIngameEventAsync(IngameEventType messageType)
        {
            IngameAsset_t asset;

            switch (messageType)
            {
                case IngameEventType.OPENING_INSTANTIATE_TITLE:
                    asset = await AssetManager.InstantiateGameObjectAsync(AssetCode.OP_TitleObject, true);
                    assets.Add(asset);
                    titleObject = asset.GetComponent<UITitleObject>();
                    inputTarget = InputOpening.OPENING_OBJECT;
                    break;
                case IngameEventType.OPENING_LOAD_TITLE_MENU:
                    asset = await AssetManager.InstantiateGameObjectAsync(AssetCode.UI_TitleMenuObject, true);
                    assets.Add(asset);
                    uiTitleMenuObject = asset.GetComponent<UITitleMenuObject>();
                    inputTarget = InputOpening.UI_TITLE_MENU_OBJECT;
                    break;
                case IngameEventType.OPENING_SELECT_NEW_GAME:
                    uiTitleMenuObject.WaitUpdate();
                    inputTarget = InputOpening.NONE;
                    IngameManager.AddIngameHander(IngameHandlerType.NEW_GAME);
                    break;
                default:
                    break;
            }
        }

        // Recevie Ingame Message
        public bool ReceiveIngameMessage<T>(T data) where T : struct
        {
            if (data is OnInput onInput)
            {
                var inputFlag = onInput.InputFlagValue;

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
                            default: 
                                return false;
                        }
                        ExecuteIngameEventAsync(next_event_type);
                        break;
                    default:
                        break;
                }
            }
            if (data is OnEndProcess onEndProcess
                && AssetCode.OP_TitleObject == onEndProcess.AssetCode)
            {
                ExecuteIngameEventAsync(IngameEventType.OPENING_LOAD_TITLE_MENU);
                return true;
            }

            return false;
        }

        private enum InputOpening
        {
            NONE = 0,

            OPENING_OBJECT,
            UI_TITLE_MENU_OBJECT,
        }
    }
}