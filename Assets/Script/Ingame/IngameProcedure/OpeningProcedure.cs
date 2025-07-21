namespace Script.Content
{
    using Script.Manager;
    using Script.Index;
    using Script.Interface;
    using Script.IngameMessage;
    using System.Threading.Tasks;

    public partial class OpeningProcedure : IngameProcedureBase, IMessageReceiver
    {
        public OpeningProcedure() : base()
        {
            procedureType = IngameProcedureType.OPENING;
        }
        public override async void Start()
        {
            await ExecuteIngameEventAsync(IngameEventType.OPENING_INSTANTIATE_TITLE);
        }

        protected override async Task<bool> ExecuteIngameEventAsync(IngameEventType messageType)
        {
            switch (messageType)
            {
                case IngameEventType.OPENING_INSTANTIATE_TITLE:
                    UITitleObject titleObject = await GetIngameObjectAsync<UITitleObject>(AssetCode.OP_TitleObject);
                    ingameObjects.Add(new(AssetCode.OP_TitleObject, titleObject.gameObject));
                    break;

                case IngameEventType.OPENING_LOAD_TITLE_MENU:
                    var uiTitleMenuObject = await GetIngameObjectAsync<UITitleMenuObject>(AssetCode.UI_TitleMenuObject);
                    ingameObjects.Add(new(AssetCode.UI_TitleMenuObject, uiTitleMenuObject.gameObject));
                    break;

                case IngameEventType.OPENING_SELECT_NEW_GAME:
                    uiTitleMenuObject = await GetIngameObjectAsync<UITitleMenuObject>(AssetCode.UI_TitleMenuObject);
                    uiTitleMenuObject.WaitUpdate();
                    IngameManager.AddIngameProcedure(IngameProcedureType.NEW_GAME);
                    break;

                default:
                    return false;
            }

            return true;
        }

        public async Task<bool> ReceiveIngameMessage<T>(T data) where T : struct
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

                return await ExecuteIngameEventAsync(next_event_type);
            }

            if (data is OnMoveNextEvent onMoveNextEvent)
            {
                return await ExecuteIngameEventAsync(onMoveNextEvent.EventType);
            }

            return false;
        }
    }
}