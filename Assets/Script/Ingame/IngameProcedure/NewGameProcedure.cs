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
        public override async void Start()
        {
            await ExecuteIngameEventAsync(IngameEventType.LOADING_CURTAIN_ON);
        }

        public async Task<bool> ReceiveIngameMessage<T>(T data) where T : struct
        {
            if (data is OnMoveNextEvent onNextEvent)
            {
                return await ExecuteIngameEventAsync(onNextEvent.EventType);
            }

            return false;
        }

        protected override async Task<bool> ExecuteIngameEventAsync(IngameEventType eventType)
        {
            switch (eventType)
            {
                case IngameEventType.LOADING_CURTAIN_ON:
                    UILoadingCurtainObject loadingCurtainObject = await GetIngameObjectAsync<UILoadingCurtainObject>(AssetCode.UI_LoadingCurtain);
                    ingameObjects.Add(new(AssetCode.UI_LoadingCurtain, loadingCurtainObject.gameObject));
                    IngameManager.EnqueueNextEventType(IngameEventType.OPENING_DISPOSE);
                    loadingCurtainObject.On(true);
                    // IngameManager에서 NextEventType을 들고 있으면 어떨까요?
                    break;

                case IngameEventType.OPENING_DISPOSE:
                    IngameManager.RemoveIngameProcedure(IngameProcedureType.OPENING);
                    goto case IngameEventType.NEWGAME_INIT_FIELD;

                case IngameEventType.NEWGAME_INIT_FIELD:
                    UnityEngine.Debug.Log($"[NewGameProcedure] NEWGAME_INIT_FIELD");
                    IngameManager.AddNewPlayData();
                    IngameManager.AddIngameProcedure(IngameProcedureType.FIELD);
                    //break;
                    goto case IngameEventType.LOADING_CURTAIN_OFF;

                case IngameEventType.LOADING_CURTAIN_OFF:
                    loadingCurtainObject = await GetIngameObjectAsync<UILoadingCurtainObject>(AssetCode.UI_LoadingCurtain);
                    loadingCurtainObject.On(false);
                    break;

                default:
                    return false;
            }

            return true;
        }
    }
}