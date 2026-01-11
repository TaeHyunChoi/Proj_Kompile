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

        private State state;

        public override async Awaitable Intialize()
        {
            state = State.NONE;
            await PlayOpening();
        }
        public async Awaitable PlayOpening()
        {
            var obj = await AssetSystem.GetOrNewInstanceAsync(PrefabID.OpeningPlayObject);
            openingPlay = obj.GetComponent<OpeningPlayObject>();


            state = State.LOAD_DATA;
            Awaitable loadTask = AssetSystem.LoadAllDatatable();
            await loadTask;


            state = State.OPENING_SEQUENCE;
            await openingPlay.PlaySequence(); // 입력에서 skip 여차저차
            // demo ...
            // ...
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