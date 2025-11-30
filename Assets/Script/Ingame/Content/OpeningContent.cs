namespace Script.Content
{
    using Script.Content;
    using Script.Index;
    using Script.Interface;
    using Script.Manager;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    public class OpeningContent : IContentState
    {
        private CancellationTokenSource skipToken;

        private TitleObject       title;
        private UITitleMenuObject titleMenu;

        public async Awaitable EnterAync()
        {
            CancellationToken token;

            // load assets
            GameObject titleObject = await AssetManagerV2.GetOrNewInstanceAsync(PrefabID.OP_TITLE_OBJECT, IngameManager.UIOverayRootTransform);
            title = titleObject.GetComponent<TitleObject>();

            // opening sequence: play logo
            token = RefreshSkipToken();
            try
            {
                await title.PlayLogoSequence(token);
            }
            catch (OperationCanceledException)
            {
                await title.ExitLogoSequence();
            }
            //token = RefreshSkipToken();

            // opening sequence: play demo
            //token = RefreshSkipToken();
            { 
                // ...
            }

            // opening sequence: play title
            await title.PlayTitleSequence();

            // title menu sequence
            GameObject uiTitleMenuObject = await AssetManagerV2.GetOrNewInstanceAsync(PrefabID.UI_TITLE_MENU_OBJECT, IngameManager.UIOverayRootTransform);
            titleMenu = uiTitleMenuObject.GetComponent<UITitleMenuObject>();

            IngameUpdateManager.Register(this);

        }

        private CancellationToken RefreshSkipToken()
        {
            if (null != skipToken)
            {
                skipToken?.Dispose();
            }
            skipToken = new CancellationTokenSource();
            return skipToken.Token;
        }

        public void Exit()
        {
            AssetManagerV2.ReleaseInstance(PrefabID.OP_TITLE_OBJECT, title.gameObject);
            AssetManagerV2.ReleaseInstance(PrefabID.UI_TITLE_MENU_OBJECT, titleMenu.gameObject);

            IngameUpdateManager.Unregister(this);

            if (null != skipToken)
            {
                skipToken.Dispose();
                skipToken = null;
            }
        }

        public void OnUpdate()
        {
            titleMenu.OnUpdate();
        }
    }
}