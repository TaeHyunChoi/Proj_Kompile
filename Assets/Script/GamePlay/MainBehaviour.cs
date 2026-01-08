namespace Script.GamePlay
{
    using Script.Data;
    using Script.GameSystem;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class MainBehaviour : MonoBehaviour
    {
        private static MainBehaviour instance;
        public static MainBehaviour Instance => instance;

        private GameplayInputSystem inputSystem;
        private List<ManagerBase> managers;


        private Dictionary<Type, ISystem> systems;

        public class OpeningSystem
        {
            public readonly GameplayInputSystem Input;

            public OpeningSystem(GameplayInputSystem inputSystem)
            {
                Input = inputSystem;
            }
        }


        private void Awake()
        {
            if (null != instance)
            {
                Destroy(this.gameObject);
                return;
            }
            instance = this;

            systems = new Dictionary<Type, ISystem>();
            inputSystem = new GameplayInputSystem();
            systems.Add(typeof(GameplayInputSystem), inputSystem);

            managers    = new List<ManagerBase>();
        }

        private async void Start()
        {
            var openingMgr = new OpeningTitleManager();
            await openingMgr.Intialize(systems);

            managers.Add(openingMgr);
        }

        private void Update()
        {
            // 입력값 갱신
            var inputFlag = inputSystem.InputFlag;

            // 매니저 업데이트
            bool inputReceived = false;
            for (int i = managers.Count - 1; i >= 0; --i)
            {
                // 입력:
                if (false == inputReceived)
                {
                    // 입력 처리: 한 번이라도 입력 처리가 되었으면 이후 순번은 입력을 받지 않음
                    inputReceived |= managers[i].OnInputReceive(inputFlag);
                }

                // 업데이트: 
                managers[i].OnUpdate();
            }
        }

        // 매니저 관리하는 클래스가 있으면 좋겠는데요?
        // 정말로 그럴까? 한 번 더 고민
    }
}