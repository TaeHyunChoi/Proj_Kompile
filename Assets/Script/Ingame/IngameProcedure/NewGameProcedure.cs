namespace Script.Content
{
    using Script.Index;
    using Script.Manager;

    public class NewGameProcedure : IngameProcedureBase
    {
        private UILoadingCurtainObject loadingCurtainObject;

        // Constructor
        public NewGameProcedure() : base()
        {
            procedureType = IngameProcedureType.NEW_GAME;
            //MessageManager.AddReceiver(this);
            //ExecuteIngameEventAsync(IngameEventType.LOADING_CURTAIN_ON);
            ExecuteIngameEventAsync(IngameEventType.NEWGAME_INIT_PLAYER);
        }


        // Execute Event
        protected override async void ExecuteIngameEventAsync(IngameEventType messageType)
        {
            switch (messageType)
            {
                case IngameEventType.NEWGAME_INIT_PLAYER:
                    IngameManager.InitPlayData();
                    goto case IngameEventType.NEWGAME_INIT_FIELD;

                case IngameEventType.NEWGAME_INIT_FIELD:
                    // 여기서 필드를 만든다는거네..
                    // 필드면 필드 핸들러 만들어서 처리하는 게 맞지 않을까?
                    //await IngameManager.TryInitializeField();
                    // 여기서 이벤트를 날리면 되니?..
                    // MessageManager.Publish(new OnEndProcess(IngameType.NewGameHandler)); 
                    // 이걸 받으면 (1) OpneingHandler.Dispose(); (2) LoadingCurtain.On(false);
                    // 이런 식으로 빠질 수 있으려나? 
                    // 가독성을 더 높일 수 있나?
                    break;
                default:
                    break;
            }
        }
    }
}