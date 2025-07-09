using Script.Index;
using Script.Interface;

namespace Script.Content
{
    public class LoadingProcedure : IngameProcedureBase
    {
        private UILoadingCurtainObject curtainObject; // 얘를 캐싱해두려면 무엇을 어떻게?
        private IngameProcedureType nextProcedureType;

        public LoadingProcedure(IngameProcedureType next_handler_type) : base()
        {
            this.nextProcedureType = next_handler_type;
            ExecuteIngameEventAsync(IngameEventType.LOADING_CURTAIN_ON);
        }

        // async 인데 void 반환하려니 불-편
        protected override async void ExecuteIngameEventAsync(IngameEventType messageType)
        {
            switch (messageType)
            {
                case IngameEventType.LOADING_CURTAIN_ON:
                    curtainObject = await GetIngameObjectAsync<UILoadingCurtainObject>(AssetCode.UI_LoadingCurtain);
                    curtainObject.On(true);
                    break;
            }
        }
    }
}
