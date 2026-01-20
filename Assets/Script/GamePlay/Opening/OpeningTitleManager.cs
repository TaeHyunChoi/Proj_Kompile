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
        // uiLoadDataListObject
        // uiOption -> 이건 Main.Systems.UIOption 으로 가야 할까?
        
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
            
            var openingObj = await AssetSystem.GetOrNewInstanceAsync(PrefabID.OpeningPlayObject, Main.UI.GamePlayRoot);
            openingPlay = openingObj.GetComponent<OpeningPlayObject>();
            
            // 모든 데이터 테이블을 로드할 때까지 다른 루프 또는 입력을 막겠다.
            await loadTask;
            
            // opening sequence: 입력을 받아 시퀀스를 skip할 수 있다;
            state = State.OPENING_SEQUENCE;
            await openingPlay.PlaySequence();

            var uiTitleMenuObj = await AssetSystem.GetOrNewInstanceAsync(PrefabID.UI_TitleMenuObject, Main.UI.PopupRoot);
            uiTitleMenu = uiTitleMenuObj.GetComponent<UITitleMenuObject>();
            state = State.TITLE_MENU;
        }

        public override bool OnInputReceive(Data.DataType.InputState inputState)
        {
            if (State.OPENING_SEQUENCE == state)
            {
                return openingPlay.SkipSequence(inputState);
            }

            if (State.TITLE_MENU == state)
            {
                int selectedIndex = uiTitleMenu.Select(inputState);
                switch (selectedIndex)
                {
                    case 0: // 처음부터 
                        Main.StartNewGame();
                        break;
                    case 1:// 이어하기 

                        break;
                    case 2:// 환경설정 

                        break;
                    case 3:// 종료하기 
#if UNITY_EDITOR

#else

#endif
                        break;
                }

                return -1 != selectedIndex;
            }

            return false;
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
            AssetSystem.ReleaseInstance(openingPlay, true);
            AssetSystem.ReleaseInstance(uiTitleMenu, true);
        }
    }
}