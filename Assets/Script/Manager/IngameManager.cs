namespace Script.Manager
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Index;
    using Script.Content;

    public class IngameManager : MonoBehaviour
    {
        private static IngameManager  instance;
        private static List<IngameLogicBase> ingameLogics;
        private static InputManager   inputMgr;

        // Ingame Logic
        public static void AddIngame(IngameLogicBase targetIngame)
        {
            ingameLogics.Add(targetIngame);
        }

        // MonoBehaviour
        private void Awake()
        {
            // like singleton
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            ingameLogics = new List<IngameLogicBase>();
            inputMgr     = new InputManager();
        }

        private void Update()
        {
            // update: contents
//            for (int i = 0; i < updates.Count; ++i)
//            {
//                // 임시 처리 - 풀링 고려 중.
//                if (null == updates[i])
//                {
//                    continue;
//                }

//                ETaskState state = updates[i].Run();

//                switch (state)
//                {
//                    case ETaskState.SUCCESS:
//                        updates[i] = null;
//                        break;
//                    case ETaskState.FAILURE:
//#if TEST_BUILD
//                        DevError.DebugAssert(ErrorCode.FAIL_TASK, updates[i].Type.ToString());
//#endif
//                        break;
//                    default:
//                        // Running
//                        break;
//                }
//            }
        }

        private void OnEnable()
        {
            inputMgr.OnEnable();
        }
        private void OnDisable()
        {
            inputMgr.OnDisable();
        }
    }
}

