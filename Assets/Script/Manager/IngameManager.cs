namespace Script.Manager
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Index;
    using Script.Content;
    using Script.Interface;

    public class IngameManager : MonoBehaviour
    {
        private static IngameManager  instance;
        private static OpeningManager openingMgr;
        private static InputManager inputMgr;

        private static List<ContentTaskContainer> updates;
        private static List<ContentTaskContainer> fixedUpdates;

        private static ITaskInput inputTarget;

        // Content Task
        private static TaskType GetTaskGroup(TaskType taskType)
        {
            int temp = (int)taskType % 1000;
            return (TaskType)((int)taskType - temp);
        }
        public static void AddTask(TaskType taskType, TaskUpdateType taskUpdateType)
        {
            ContentTaskContainer task;
            bool isTaskAdded;

            // content type
            TaskType taskGroupType = GetTaskGroup(taskType);
            switch (taskGroupType)
            {
                case TaskType.OPENGING:
                    isTaskAdded = openingMgr.TryAddTask(taskType, out task);
                    break;

                default:
                    OnlyDev.DevError.DebugAssert(ErrorCode.CANNOT_FIND_TASK_GROUP, taskGroupType.ToString());
                    return;
            }

            if (false == isTaskAdded)
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
                default:
                    OnlyDev.DevError.DebugAssert(ErrorCode.CANNOT_FIND_TASK_UPDATE_TYPE, taskUpdateType.ToString());
                    break;
            }
        }

        // Input
        public static void SetInputTarget(ITaskInput target)
        {
            inputTarget = target;
        }
        public static void SetInputValue(IDxInput.EInputFlag inputFlag)
        {
            //상시 옵션 값을 걸어도 될 것 같고..
            //if(true == option.InputValue(inputFlag))
            //  { return; }

            // 그 다음에 input target에게 전달?
            inputTarget.InputValue(inputFlag);
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

                IETaskState state = updates[i].Run();

                switch (state)
                {
                    case IETaskState.SUCCESS:
                        updates[i] = null;
                        break;
                    case IETaskState.FAILURE:
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
            // inputFlag는 Update()에서 받았음

            for (int i = 0; i < fixedUpdates.Count; ++i)
            {
                IETaskState state = fixedUpdates[i].Run();

                switch (state)
                {
                    case IETaskState.SUCCESS:
                        fixedUpdates[i] = null;
                        break;
                    case IETaskState.FAILURE:
                        UnityEngine.Assertions.Assert.IsTrue(state == IETaskState.FAILURE);
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

