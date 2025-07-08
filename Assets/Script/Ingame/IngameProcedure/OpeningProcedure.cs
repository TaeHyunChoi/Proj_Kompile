namespace Script.Content
{
    using Script.Manager;
    using Script.Index;
    using Script.Interface;
    using Script.IngameMessage;
    using static UITitleMenuObject.MenuType;

    public partial class OpeningProcedure : IngameProcedureBase, IMessageReceiver
    {
        private UITitleObject     titleObject;
        private UITitleMenuObject uiTitleMenuObject;
        private InputOpening      inputTarget;

        public OpeningProcedure() : base()
        {
            procedureType = IngameProcedureType.OPENING;
            inputTarget = InputOpening.NONE;

            ExecuteIngameEventAsync(IngameEventType.OPENING_INSTANTIATE_TITLE);
        }

        // Execute Event
        protected override async void ExecuteIngameEventAsync(IngameEventType messageType)
        {
            switch (messageType)
            {
                case IngameEventType.OPENING_INSTANTIATE_TITLE:
                    titleObject = await CreateIngameObjectAsync<UITitleObject>(AssetCode.OP_TitleObject);
                    inputTarget = InputOpening.OPENING_OBJECT;
                    break;
                case IngameEventType.OPENING_LOAD_TITLE_MENU:
                    uiTitleMenuObject = await CreateIngameObjectAsync<UITitleMenuObject>(AssetCode.UI_TitleMenuObject);
                    inputTarget = InputOpening.UI_TITLE_MENU_OBJECT;
                    break;
                case IngameEventType.OPENING_SELECT_NEW_GAME:
                    // 여기서부터의 절차를 어떻게 하면 좋을까?에 대한 고민이 필요하겠군.
                    // 핸들러가 여러 개 쓰이는 상황이다. => IngameManager로 넘기는 게 맞지 않나?
                    // 좋은 '규칙' 뭐 없나...
                    uiTitleMenuObject.WaitUpdate();
                    inputTarget = InputOpening.NONE;
                    IngameManager.AddIngameHander(IngameProcedureType.LOADING);
                    //IngameManager.AddIngameHander(IngameHandlerType.NEW_GAME);
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
            if (data is OnEndProcess onEndProcess)
            {
                if (AssetCode.OP_TitleObject == onEndProcess.AssetCode)
                {
                    ExecuteIngameEventAsync(IngameEventType.OPENING_LOAD_TITLE_MENU);
                    return true;
                }
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