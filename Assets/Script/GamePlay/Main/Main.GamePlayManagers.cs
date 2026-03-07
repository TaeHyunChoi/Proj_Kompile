namespace Script.GamePlay
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public partial class Main
    {
        public class GamePlayManagers
        {
            // manager 개수가 많지 않으므로 이정도는 부담 없다.
            private readonly List<ManagerBase> list = new List<ManagerBase>();                          // 빠른 검색을 위해 Dictionary에 등록
            private readonly Dictionary<Type, ManagerBase> dict = new Dictionary<Type, ManagerBase>();  // LIFO 업데이트를 위해 List에 등록

            public void Add<T>(T manager) where T : ManagerBase
            {
                var type = typeof(T);

                if (true == dict.ContainsKey(type))
                {
                    Debug.LogWarning($"[GamePlayManagers] {type.Name} is already registered.");
                    return;
                }

                dict.Add(type, manager);
                list.Add(manager);
            }
            public T Get<T>() where T : ManagerBase
            {
                if (true == dict.TryGetValue(typeof(T), out var manager))
                {
                    return manager as T;
                }

                return null;
            }
            public void Remove<T>() where T : ManagerBase
            {
                var type = typeof(T);
                if (dict.TryGetValue(type, out var manager))
                {
                    manager.Dispose();

                    dict.Remove(type);
                    list.Remove(manager);
                }
            }


            public void OnUpdateAll(Data.DataType.InputState inputState)
            {
                bool inputReceived = false;

                for (int i = list.Count - 1; i >= 0; --i)
                {
                    var manager = list[i];

                    if (false == inputReceived)
                    {
                        // 입력 처리가 되면 true를 반환하여 inputReceived를 true로 만듦
                        // 이후 순번의 매니저는 입력을 받지 못함
                        inputReceived = manager.OnInputReceive(inputState);
                    }

                    manager.OnUpdate();
                }
            }


            // 받을 때에 PlayData를 넣어줘야 하니까...
            public async void NewGame()
            {
                // 로딩 커튼 on
                await UI.SetLoadingCurtain(true);

                // 오프닝-타이틀 매니저 해제
                Remove<OpeningTitleManager>();

                // 필드 매니저 생성 및 추가
                //var fieldMgr = new FieldManager(new Data.PlayData());
                //managers.Add(fieldMgr);
                //await fieldMgr.Intialize();
                // FieldInfo, GamePlayData, Player, NPC

                // 필드 HUD 초기화
                // ...

                // 필드 시작
                // FieldManager.Play();

                // 로딩 커튼 off
                await UI.SetLoadingCurtain(false);
            }
        }
    }
}