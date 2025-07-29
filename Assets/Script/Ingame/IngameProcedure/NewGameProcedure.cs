namespace Script.Content
{
    using Script.Index;
    using Script.IngameMessage;
    using Script.Interface;
    using Script.Manager;
    using System.Threading.Tasks;

    public class NewGameProcedure : IngameProcedureBase, IMessageReceiver
    {
        public NewGameProcedure() : base()
        {
            procedureType = IngameProcedureType.NEW_GAME;
        }

        public override async Task<bool> Start()
        {
            return await ExecuteIngameEventAsync(IngameEventType.LOADING_CURTAIN_ON);
        }

        public async Task<bool> ReceiveIngameMessage<T>(T data) where T : struct
        {
            if (data is OnEndEvent onEndEvent)
            {
                IngameEventType nextEventType;
                switch (onEndEvent.EventType)
                {
                    case IngameEventType.LOADING_CURTAIN_ON:   nextEventType = IngameEventType.OPENING_DISPOSE;       break;
                    case IngameEventType.FIELD_INIT:           nextEventType = IngameEventType.LOADING_CURTAIN_OFF;   break;
                    case IngameEventType.LOADING_CURTAIN_OFF:  nextEventType = IngameEventType.NEWGAME_DISPOSE;       break;
                    default:
                        return false;
                }

                return await ExecuteIngameEventAsync(nextEventType);
            }

            return false;
        }

        protected override async Task<bool> ExecuteIngameEventAsync(IngameEventType eventType)
        {
            switch (eventType)
            {
                case IngameEventType.LOADING_CURTAIN_ON:
                    UILoadingCurtainObject loadingCurtainObject = await GetIngameObjectAsync<UILoadingCurtainObject>(AssetCode.UI_LoadingCurtain, AssetParentType.CANVAS_OVERAY);
                    ingameObjects.Add(new(AssetCode.UI_LoadingCurtain, loadingCurtainObject.gameObject));
                    loadingCurtainObject.On(true);
                    break;

                case IngameEventType.OPENING_DISPOSE:
                    IngameManager.RemoveIngameProcedure(IngameProcedureType.OPENING);
                    goto case IngameEventType.NEWGAME_INITIALIZE;

                case IngameEventType.NEWGAME_INITIALIZE:
                    UnityEngine.Debug.Log($"[NewGameProcedure] NEWGAME_INIT_FIELD");
                    IngameManager.AddNewPlayData();
                    IngameManager.AddIngameProcedure(IngameProcedureType.FIELD);
                    break;

                case IngameEventType.LOADING_CURTAIN_OFF:
                    loadingCurtainObject = await GetIngameObjectAsync<UILoadingCurtainObject>(AssetCode.UI_LoadingCurtain, AssetParentType.CANVAS_OVERAY);
                    loadingCurtainObject.On(false);
                    break;
                case IngameEventType.NEWGAME_DISPOSE:
                    IngameManager.RemoveIngameProcedure(IngameProcedureType.NEW_GAME);
                    break;

                default:
                    return false;
            }

            return true;
        }
    }
}