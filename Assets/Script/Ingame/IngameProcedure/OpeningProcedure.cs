namespace Script.Content
{
    using Script.Index;
    using Script.Interface;
    using Script.IngameMessage;
    
    public partial class OpeningProcedure : IngameProcedureBase, IMessageReceiver
    {
        public OpeningProcedure() : base()
        {
            procedureType = IngameProcedureType.OPENING;
            ExecuteIngameEventAsync(IngameEventType.OPENING_INSTANTIATE_TITLE);
        }

        // Execute Event
        protected override async void ExecuteIngameEventAsync(IngameEventType messageType)
        {
            switch (messageType)
            {
                case IngameEventType.OPENING_INSTANTIATE_TITLE:
                    UITitleObject titleObject = await GetIngameObjectAsync<UITitleObject>(AssetCode.OP_TitleObject);
                    ingameObjects.Add(new (AssetCode.OP_TitleObject, titleObject.gameObject));
                    break;
                case IngameEventType.OPENING_LOAD_TITLE_MENU:
                    var uiTitleMenuObject = await GetIngameObjectAsync<UITitleMenuObject>(AssetCode.UI_TitleMenuObject);
                    ingameObjects.Add(new(AssetCode.UI_TitleMenuObject, uiTitleMenuObject.gameObject));
                    break;
                case IngameEventType.OPENING_SELECT_NEW_GAME:
                    uiTitleMenuObject = await GetIngameObjectAsync<UITitleMenuObject>(AssetCode.UI_TitleMenuObject);
                    uiTitleMenuObject.WaitUpdate();

                    // 여기서 스위칭을 더 잘하면 좋을 것 같은데
                    //IngameManager.AddIngameProcedure(IngameProcedureType.LOADING);
                    break;
                default:
                    break;
            }
        }

        // Recevie Ingame Message
        public bool ReceiveIngameMessage<T>(T data) where T : struct
        {
            if (data is OnSelect_UITitleMenu onSelectMenu)
            {
                var menuType = onSelectMenu.ValueInt;
                IngameEventType next_event_type;

                switch (menuType)
                {
                    case 0: next_event_type = IngameEventType.OPENING_SELECT_NEW_GAME;  break;
                    case 1: next_event_type = IngameEventType.OPENING_SELECT_LOAD_GAME; break;
                    case 2: next_event_type = IngameEventType.OPENING_SELECT_OPTION;    break;
                    case 3: next_event_type = IngameEventType.OPENING_SELECT_EXIT;      break;
                    default:
                        return false;
                }

                ExecuteIngameEventAsync(next_event_type);
                return true;
            }

            // OnEnd 라고 하니까 헷갈림.
            // MoveNext()라고 표현하는게 더 좋을 듯
            // 그렇다면? IngameEventType.OPENING_SELECT_NEW_GAME 에서도 완료 후 Message.MoveNext(CLOSE_OPENING_PROCEDURE); 식으로 하는게 좋을 듯
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
    }
}