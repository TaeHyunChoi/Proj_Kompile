namespace Script.GamePlay
{
    using Script.Data;
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
        // private UITitleMenuObject uiTitleMenu;
        
        private State state;

        public override async Awaitable Intialize()
        {
            state = State.NONE;
            await PlayOpening();
        }
        public async Awaitable PlayOpening()
        {
            var obj = await AssetSystem.GetOrNewInstanceAsync(PrefabID.OpeningPlayObject, Main.UI.OverlayCanvas);
            openingPlay = obj.GetComponent<OpeningPlayObject>();


            state = State.LOAD_DATA;
            Awaitable loadTask = AssetSystem.LoadAllDatatable();
            await loadTask;
            
            // opening sequence: 입력을 받아 시퀀스를 skip할 수 있다;
            state = State.OPENING_SEQUENCE;
            await openingPlay.PlaySequence();

            // title menu: 입력을 받아서 여차저차;
            // ...
        }

        public override bool OnInputReceive(Data.DataType.InputState inputState)
        {
            switch (state)
            {
                case State.OPENING_SEQUENCE:
                    return openingPlay.SkipSequence(inputState);
                case State.TITLE_MENU:
                    // 입력 조작
                    break;
            }
            return true;
        }

        public override bool OnUpdate()
        {
            return true;
        }
        public override void Dispose()
        {
            AssetSystem.ReleaseInstance(openingPlay);
        }
    }
}