namespace Script.Content
{
    using Script.Index;
    using Script.Interface;
    using Script.IngameMessage;
    using Script.Manager;

    public class NewGameHandler : _IngameHandlerBase, IMessageReceiver
    {
        private UILoadingCurtainObject loadingCurtainObject;

        // Constructor
        public NewGameHandler() : base()
        {
            handlerType = IngameHandlerType.NEW_GAME;
            MessageManager.AddReceiver(this);
            ExecuteIngameEventAsync(IngameEventType.LOADING_CURTAIN_ON);
        }


        // Execute Event
        protected override async void ExecuteIngameEventAsync(IngameEventType messageType)
        {
            switch (messageType)
            {
                case IngameEventType.LOADING_CURTAIN_ON:
                    IngameAsset_t asset = await AssetManager.InstantiateGameObjectAsync(AssetCode.UI_LoadingCurtain, true);
                    assets.Add(asset);

                    loadingCurtainObject = asset.GetComponent<UILoadingCurtainObject>();
                    loadingCurtainObject.On(true);
                    goto case IngameEventType.NEWGAME_INIT_PLAYER;

                case IngameEventType.NEWGAME_INIT_PLAYER:
                    IngameManager.InitPlayData();
                    goto case IngameEventType.NEWGAME_INIT_FIELD;

                case IngameEventType.NEWGAME_INIT_FIELD:
                    // 필드 초기화
                    await IngameManager.TryInitializeField();
                    // 얘는 그냥 message.Receive() 받아서 다음 단계로 넘어가면 될 듯
                    // strcut OnProceedNextEvent(handler_type, IngameEventType type)으로 넘겨서 받으면 될 듯?
                    // 코드 가독성을 높이기 위해 '여기에' message.Publish<T>를 남기면 좋겠다.

                    //while (false == loadingCurtainObject.IsOn)
                    //{
                    //    await Task.Yield();
                    //}
                    //goto case IngameEventType.LOADING_CURTAIN_OFF;
                    break;

                case IngameEventType.LOADING_CURTAIN_OFF:
                    
                    // await Task.Delay(200);
                    // 이거 쓸바엔 코루틴 같은거 만들어서 .Update()로 대기 및 message.Receive()로 받는게 나을 듯
                    // 그러면 '다음 단계로 넘어가주세요' struct OnEventNext() 를 만들면 되려나?

                    loadingCurtainObject.On(false);
                    break;
                default:
                    break;
            }
        }
        public bool ReceiveIngameMessage<T>(T data) where T : struct
        {
            if (data is OnEndLoadingCurtain onEndLoadingCurtain)
            {
                if (true == onEndLoadingCurtain.isOn)
                {
                    IngameManager.RemoveIngameHandler(IngameHandlerType.OPENING);
                }
                else
                {
                    IngameManager.RemoveIngameHandler(handlerType);
                }

                return true;
            }

            return false;
        }

        // Dispose
        // base class: _IngameHandlerBase.Dispose();
    }
}