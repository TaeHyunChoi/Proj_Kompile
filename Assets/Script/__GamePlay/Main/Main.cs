namespace Script.GamePlay
{
    using System;
    using UnityEngine;
    using Script.Asset.Provider;

    public partial class Main : MonoBehaviour
    {
        private static Main     instance;
        
        // 시스템 모음 (asset, input, sound, ...)
        // 예외적으로 SystemUI를 넣어서 UIOption을 여기서 처리하는 것도 방법일 듯 하다.
        private static GamePlaySystem   systems;

        // 매니저 일괄 관리
        private static GamePlayManagers managers;

        // ui canvas : 얘도 이렇게 받는게 좀 짜침 => 그냥 매개변수로 넘기자
        private UISystem uiCanvas;

        public static UISystem UI => instance.uiCanvas;
        
        // MonoBehaviours
        private void Awake()
        {
            if (null != instance)
            {
                Destroy(this.gameObject);
                return;
            }
            instance = this;

            systems  = new GamePlaySystem();
            managers = new GamePlayManagers();

            Transform uiRoot = transform.Find("UI");
            uiCanvas = new UISystem(uiRoot);

            AssetRepoProvider.Initialize();
        }
        private async void Start()
        {
            try
            {
                var openingMgr = new OpeningTitleManager();
                managers.Add(openingMgr);
                await openingMgr.Intialize();
            }
            catch (Exception)
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
            managers.OnUpdateAll(inputState);

            // 프레임 처리가 다 끝난 후, 현재 입력을 '이전'으로 백업
            systems.Input.OnEndOfFrame();
        }


        // Managers
        public static void StartNewGame() => managers.NewGame();

    }
}