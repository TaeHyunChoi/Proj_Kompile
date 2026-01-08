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
        GameplayInputSystem inputSystem;


        public override async Awaitable Intialize(Dictionary<Type, ISystem> systems)
        {
            inputSystem = systems[typeof(GameplayInputSystem)] as GameplayInputSystem;
            inputSystem.Reset();

            Awaitable loadTask = AssetSystem.LoadAllDatatable();
            
            var obj = await AssetSystem.GetOrNewInstanceAsync(PrefabID.OpeningPlayObject);
            openingPlay = obj.GetComponent<OpeningPlayObject>();
            Awaitable playTask = openingPlay.Play();

            // 데이터테이블 불러오기가 완료될 때까지 일단 대기해
            await loadTask;

            try
            {

            }
            catch (OperationCanceledException)
            {
                inputSystem.Reset();

                // openingPlay 단계에 따라서 여차저차.
            }
        }

        public override bool OnInputReceive(Data.DataType.IDxInput inputFlag)
        {
            return true;
        }

        public override bool OnUpdate()
        {
            return true;
        }
    }

}