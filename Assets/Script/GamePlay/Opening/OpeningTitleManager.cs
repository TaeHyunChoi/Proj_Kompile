namespace Script.GamePlay
{
    using UnityEngine;
    using Script.Asset;

    public class OpeningTitleManager : ManagerBase
    {
        private enum State
        { 
            NONE,
            LOAD_DATA,
            OPENING_SEQUENCE,
            TITLE_MENU,
        }

        private OpeningPlayObject openingPlay;
        private UITitleMenuObject uiTitleMenu;
        
        private State state;

        public override async Awaitable Intialize()
        {
            state = State.NONE;
            await PlayOpening();
        }
        public async Awaitable PlayOpening()
        {
            state = State.LOAD_DATA;
            Awaitable loadTask = AssetSystem.LoadAllDatatable();
            
            var openingObj = await AssetSystem.GetOrNewInstanceAsync(PrefabID.OpeningPlayObject, Main.UI.OverlayCanvas);
            openingPlay = openingObj.GetComponent<OpeningPlayObject>();
            
            // 모든 데이터 테이블을 로드할 때까지 다른 루프 또는 입력을 막겠다.
            await loadTask;
            
            // opening sequence: 입력을 받아 시퀀스를 skip할 수 있다;
            state = State.OPENING_SEQUENCE;
            await openingPlay.PlaySequence();

            var uiTitleMenuObj = await AssetSystem.GetOrNewInstanceAsync(PrefabID.UI_TitleMenuObject, Main.UI.OverlayCanvas);
            uiTitleMenu = uiTitleMenuObj.GetComponent<UITitleMenuObject>();
            state = State.TITLE_MENU;
        }

        public override bool OnInputReceive(Data.DataType.InputState inputState)
        {
            switch (state)
            {
                case State.OPENING_SEQUENCE:
                    return openingPlay.SkipSequence(inputState);
                case State.TITLE_MENU:
                    return uiTitleMenu.Select(inputState);
                    break;
            }
            return true;
        }

        public override bool OnUpdate()
        {
            if (State.TITLE_MENU == state)
            {
                uiTitleMenu.OnUpdate();
            }

            return true;
        }
        public override void Dispose()
        {
            AssetSystem.ReleaseInstance(openingPlay);
        }
    }
}