namespace Script.GamePlay
{
    using System;
    using Script.Data;
    using System.Collections.Generic;
    using UnityEngine;
    using Script.GameSystem;
    using Script.Asset;

    public class OpeningTitleManager : ManagerBase
    {
        private OpeningPlayObject openingPlay;
        // x_UITitleMenuObject

        private GameplayInputSystem inputSystem;

        public override async Awaitable Intialize(Dictionary<Type, ISystem> systems)
        {
            inputSystem = systems[typeof(GameplayInputSystem)] as GameplayInputSystem;
            inputSystem.Reset();

            Awaitable loadTask = AssetSystem.LoadAllDatatable();
            
            var obj = await AssetSystem.GetOrNewInstanceAsync(PrefabID.OpeningPlayObject);
            openingPlay = obj.GetComponent<OpeningPlayObject>();

            // 데이터테이블 불러오기가 완료될 때까지 일단 대기해
            await loadTask;

            try
            {
                await openingPlay.Play(inputSystem);
            }
            catch (OperationCanceledException)
            {
                await openingPlay.ExitLogoSequence(inputSystem);
            }


        }

        public override bool OnInputReceive(DataType.IDxInput inputFlag)
        {
            return true;
        }

        public override bool OnUpdate()
        {
            return true;
        }
    }

}