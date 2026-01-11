namespace Script.GamePlay
{
    using Script.Asset;
    using Script.Data;
    using Script.GameSystem;
    using UnityEngine;

    public partial class Main
    {
        /// <summary> UI 생성 시, 부모 캔버스 설정을 위함. 이외의 기능은 상정하지 않음 </summary>
        public class UISystem : MonoBehaviour
        {
            // Canvas Transform Root
            private readonly Transform camRoot;
            private readonly Transform gameplayRoot;
            private readonly Transform popupRoot;
            private readonly Transform systemRoot;
            private readonly Transform loadingRoot;

            public Transform CamRoot      => camRoot;
            public Transform GamePlayRoot => gameplayRoot;
            public Transform PopupRoot    => popupRoot;
            public Transform SystemRoot   => systemRoot;
            public Transform LoadingRoot  => loadingRoot;

            public UISystem(Transform root)
            {
                camRoot     = root.GetChild(0);
                gameplayRoot   = root.GetChild(1);
                popupRoot = root.GetChild(2);
                systemRoot  = root.GetChild(3);
                loadingRoot = root.GetChild(4);
            }

            public async Awaitable SetLoadingCurtain(bool on)
            {
                var obj = await AssetSystem.GetOrNewInstanceAsync(PrefabID.UI_LoadingCurtain, UI.LoadingRoot);
                var loading = obj.GetComponent<UILoadingCurtain>();
                await loading.SetLoadingCurtain(on);

                if (false == on)
                {
                    AssetSystem.ReleaseInstance(loading);
                }
            }
        }        
    }
}