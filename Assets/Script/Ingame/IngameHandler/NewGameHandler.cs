namespace Script.Content
{
    using Script.Data;
    using Script.Index;
    using Script.Interface;
    using Script.IngameMessage;
    using Script.Manager;
    using UnityEngine;
    using System.Threading.Tasks;

    public class NewGameHandler : _IngameHandlerBase, IMessageReceiver
    {
        private UILoadingCurtainObject loadingCurtainObject;


        // Constructor
        public NewGameHandler()
        {
            handlerType = IngameHandlerType.NEW_GAME;
            MessageManager.AddReceiver(this, false);
            ExecuteIngameEventAsync(IngameEventType.LOADING_CURTAIN_ON);
        }

        // Execute Event
        public override async void ExecuteIngameEventAsync(IngameEventType messageType)
        {
            try
            {
                switch (messageType)
                {
                    case IngameEventType.LOADING_CURTAIN_ON:
                        loadingCurtainObject = await AssetManager.InstantiateGameObjectAsync<UILoadingCurtainObject>(AssetCode.UI_LoadingCurtain, CanvasType.OVERLAY_LOADING, true);
                        loadingCurtainObject.On(true);
                        goto case IngameEventType.NEWGAME_INIT_PLAYER;
                    
                    case IngameEventType.NEWGAME_INIT_PLAYER:
                        Debug.Log("NEWGAME_PLAYER_DATA");
                        goto case IngameEventType.NEWGAME_INIT_FIELD;

                    case IngameEventType.NEWGAME_INIT_FIELD:
                        Debug.Log("NEWGAME_INIT_FIELD");
                        break;

                    case IngameEventType.LOADING_CURTAIN_OFF:
                        await Task.Delay(500);
                        loadingCurtainObject.On(false);
                        break;
                    default:
                        break;
                }
            }
            catch
            {
                Debug.Assert(false);
            }
        }



        public bool Receive_IngameEvent<T>(IngameEventType type, T data) where T : struct
        {
            if (data is OnEndProcess onEnd
                && AssetCode.UI_LoadingCurtain == onEnd.AssetCode)
            {
                bool curtainOn = (1 == onEnd.endCode);

                if (true == curtainOn)
                {
                    IngameManager.RemoveIngameHandler(IngameHandlerType.OPENING);

                    // for test
                    ExecuteIngameEventAsync(IngameEventType.LOADING_CURTAIN_OFF);
                }
                else
                {
                    IngameManager.RemoveIngameHandler(this.handlerType);
                }

                return true;
            }

            return false;
        }
        public override void Receive_Input(IDxInput.InputFlag inputFlag)
        {
            // nothing;
        }

        public override void Dispose()
        {
            MessageManager.Dispose(this);
        }
    }
}
