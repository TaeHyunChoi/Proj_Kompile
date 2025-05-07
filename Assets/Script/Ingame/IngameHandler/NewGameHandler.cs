namespace Script.Content
{
    using Script.Index;
    using Script.Interface;
    using Script.IngameMessage;
    using Script.Manager;
    using System.Threading.Tasks;
    using UnityEngine;
    using System.Collections.Generic;

    public class NewGameHandler : _IngameHandlerBase, IMessageReceiver
    {
        private UILoadingCurtainObject loadingCurtainObject;

        // Constructor
        public NewGameHandler() : base()
        {
            handlerType = IngameHandlerType.NEW_GAME;
            MessageManager.AddReceiver(this, false);
            ExecuteIngameEventAsync(IngameEventType.LOADING_CURTAIN_ON);
        }


        // Execute Event
        public override async void ExecuteIngameEventAsync(IngameEventType messageType)
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

                    // Task.Yield();도 GC를 먹는다 => UniTask를 권장;
                    while (false == loadingCurtainObject.IsOn)
                    {
                        await Task.Yield();
                    }
                    goto case IngameEventType.LOADING_CURTAIN_OFF;

                case IngameEventType.LOADING_CURTAIN_OFF:
                    await Task.Delay(200);
                    loadingCurtainObject.On(false);
                    break;
                default:
                    break;
            }
        }
        public bool Receive_IngameEvent<T>(IngameEventType type, T data) where T : struct
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
        public override void Receive_Input(IDxInput.InputFlag inputFlag)
        {
            // nothing;
        }

        // Dispose
        // base class: _IngameHandlerBase.Dispose();
    }
}
