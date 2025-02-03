namespace Script.Manager
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Index;
    using Script.Content;

    public class IngameManager : MonoBehaviour
    {
        private static IngameManager  instance;

        private static OpeningManager openingMgr;
        private static InputManager   inputMgr;

        private static List<ContentTaskContainer> updates;
        private static List<ContentTaskContainer> fixedUpdates;
        private static List<ContentTaskContainer> lateUpdates;

        // Content Task
        private static TaskType GetTaskGroup(TaskType taskType)
        {
            int temp = (int)taskType % 1000;
            return (TaskType)((int)taskType - temp);
        }
        public static void AddTask(TaskType taskType, TaskUpdateType taskUpdateType)
        {
            ContentTaskContainer task = null;
            bool isTaskAdded;

            // content type
            TaskType taskGroupType = GetTaskGroup(taskType);
            switch (taskGroupType)
            {
                case TaskType.OPENGING:
                    isTaskAdded = openingMgr.TryAddTask(taskType, out task);
                    break;

                case TaskType.UI:
                    isTaskAdded = false; // uiMgr 만들어서 .TryAddTask(taskType, out task)를 해야 하네?
                    //isTaskAdded = uimgr.TryAddTask(taskType, out task);
                    break;

                default:
                    OnlyDev.DevError.DebugAssert(ErrorCode.CANNOT_FIND_TASK_GROUP, taskGroupType.ToString());
                    return;
            }

            if (false == isTaskAdded || null == task)
            {
                // error log 는 TryAddTask에서 찍음
                return;
            }

            switch (taskUpdateType)
            {
                case TaskUpdateType.UPDATE:
                    updates.Add(task);      
                    break;

                case TaskUpdateType.FIXED_UPDATE:
                    fixedUpdates.Add(task); 
                    break;

                case TaskUpdateType.LATE_UPDATE:
                    lateUpdates.Add(task);
                    break;

                default:
                    OnlyDev.DevError.DebugAssert(ErrorCode.CANNOT_FIND_TASK_UPDATE_TYPE, taskUpdateType.ToString());
                    break;
            }
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

            updates      = new List<ContentTaskContainer>();
            fixedUpdates = new List<ContentTaskContainer>();

            openingMgr   = new OpeningManager();
            inputMgr     = new InputManager();
        }
        private void Start()
        {
            AddTask(TaskType.OP_PLAY_OPENING, TaskUpdateType.UPDATE);
            AssetManager.Initialize(transform); // 이것도 Task 형식으로?
        }

        private void Update()
        {
            // update: contents
            for (int i = 0; i < updates.Count; ++i)
            {
                // 임시 처리 - 풀링 고려 중.
                if (null == updates[i])
                {
                    continue;
                }

                ETaskState state = updates[i].Run();

                switch (state)
                {
                    case ETaskState.SUCCESS:
                        updates[i] = null;
                        break;
                    case ETaskState.FAILURE:
#if TEST_BUILD
                        DevError.DebugAssert(ErrorCode.FAIL_TASK, updates[i].Type.ToString());
#endif
                        break;
                    default:
                        // Running
                        break;
                }
            }
        }
        private void FixedUpdate()
        {
            for (int i = 0; i < fixedUpdates.Count; ++i)
            {
                // 임시 처리
                if (null == fixedUpdates[i])
                {
                    continue;
                }

                ETaskState state = fixedUpdates[i].Run();

                switch (state)
                {
                    case ETaskState.SUCCESS:
                        fixedUpdates[i] = null;
                        break;
                    case ETaskState.FAILURE:
                        UnityEngine.Assertions.Assert.IsTrue(state == ETaskState.FAILURE);
                        break;
                    default:
                        // Running
                        break;
                }
            }
        }
        private void LateUpdate()
        {
            for (int i = 0; i < lateUpdates.Count; ++i)
            {
                // 임시 처리
                if (null == lateUpdates[i])
                {
                    continue;
                }

                ETaskState state = lateUpdates[i].Run();

                switch (state)
                {
                    case ETaskState.SUCCESS:
                        lateUpdates[i] = null;
                        break;
                    case ETaskState.FAILURE:
                        UnityEngine.Assertions.Assert.IsTrue(state == ETaskState.FAILURE);
                        break;
                    default:
                        // Running
                        break;
                }
            }
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

