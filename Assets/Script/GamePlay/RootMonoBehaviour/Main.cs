using Script.Asset;
using System;

namespace Script.GamePlay
{
    using Script.GameSystem;
    using System.Collections.Generic;
    using UnityEngine;

    public partial class Main : MonoBehaviour
    {
        private static Main     instance;
        
        private static GamePlaySystem    systems;
        private static List<ManagerBase> managers;

        // ui canvas
        private UICanvas uiCanvas;
        [SerializeField] private Transform cameraCanvas;
        [SerializeField] private Transform overlayCanvas;
        [SerializeField] private Transform curtainCanvas;

        public static UICanvas UI => instance.uiCanvas;
        
        private void Awake()
        {
            if (null != instance)
            {
                Destroy(this.gameObject);
                return;
            }
            instance = this;

            systems  = new GamePlaySystem();
            managers = new List<ManagerBase>();

            uiCanvas = new UICanvas(cameraCanvas, overlayCanvas, curtainCanvas);
                
            AssetSystem.Initialize();
        }

        private async void Start()
        {
            try
            {
                var openingMgr = new OpeningTitleManager();
                managers.Add(openingMgr);

                await openingMgr.Intialize();
            }
            catch (Exception e)
            {
                // async void 라서 예외를 잡기 위하여 try catch 구문을 사용;
                throw; // TODO 예외 처리
            }
        }

        private void Update()
        {
            // 입력값 갱신 (현재와 과거)
            Data.DataType.InputState inputState = systems.Input.Current;

            // 매니저 업데이트
            bool inputReceived = false;
            for (int i = managers.Count - 1; i >= 0; --i)
            {
                // 입력:
                if (false == inputReceived)
                {
                    // 입력 처리: 한 번이라도 입력 처리가 되었으면 이후 순번은 입력을 받지 않음
                    inputReceived |= managers[i].OnInputReceive(inputState);
                }

                // 업데이트: 
                managers[i].OnUpdate();
            }

            // 프레임 처리가 다 끝난 후, 현재 입력을 '이전'으로 백업
            systems.Input.OnEndOfFrame();
        }
    }
}