namespace Script.Manager
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Index;
    using Script.Content;
    using Script.OnlyDev;

    public class IngameManager : MonoBehaviour
    {
        // manager
        private static IngameManager  instance;
        private static OpeningManager openingMgr;

        // content task
        private static List<ContentTaskContainer> updates;
        private static List<ContentTaskContainer> fixedUpdates;

        // input
        private IDxInput.EInputFlag inputFlag;

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
#if UNITY_EDITOR || TEST_BUILD
                    OnlyDev.DevError.DebugAssert(ErrorCode.CANNOT_FIND_TASK_GROUP, taskGroupType.ToString());
#endif
                    return;
            }

            if (false == isTaskAdded)
            {
                // error log 는 TryAddTask에서 찍음
                return;
            }

            // update type : 여기서 NULL 자리 찾아서 풀링을 하는 게 좋을까?
            switch (taskUpdateType)
            {
                case TaskUpdateType.UPDATE:
                    updates.Add(task);      
                    break;
                case TaskUpdateType.FIXED_UPDATE:
                    fixedUpdates.Add(task); 
                    break;
                default:
#if UNITY_EDITOR || TEST_BUILD
                    OnlyDev.DevError.DebugAssert(ErrorCode.CANNOT_FIND_TASK_UPDATE_TYPE, taskUpdateType.ToString());
#endif
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
        }
        private void Start()
        {
            AddTask(TaskType.OP_PLAY_OPENING, TaskUpdateType.UPDATE);
            AssetManager.Initialize(transform);
        }
        private void Update()
        {
            // update: input
            inputFlag = IDxInput.TryGetInput();

            // update: contents
            for (int i = 0; i < updates.Count; ++i)
            {
                // 임시 처리
                if (null == updates[i])
                {
                    continue;
                }

                ContentTaskState state = updates[i].Run();

                switch (state)
                {
                    case ContentTaskState.SUCCESS:
                        updates[i] = null;
                        break;
                    case ContentTaskState.FAILURE:
#if UNITY_EDITOR || TEST_BUILD
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
                ContentTaskState state = fixedUpdates[i].Run();

                switch (state)
                {
                    case ContentTaskState.SUCCESS:
                        fixedUpdates[i] = null;
                        break;
                    case ContentTaskState.FAILURE:
                        UnityEngine.Assertions.Assert.IsTrue(state == ContentTaskState.FAILURE);
                        break;
                    default:
                        // Running
                        break;
                }
            }
        }
    }
}

