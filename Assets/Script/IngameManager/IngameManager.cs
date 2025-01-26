namespace Script.GameManager
{
    using UnityEngine;
    using Script.Index;
    using Script.Content;
    using System.Linq;
    using System.Collections.Generic;

    public class IngameManager : MonoBehaviour
    {
        private static IngameManager instance;

        private static List<ContentTaskContainer> updates;
        private static List<ContentTaskContainer> fixedUpdates;

        private static OpeningManager openingMgr;

        private TaskType currentTaskType;
        private IDxInput.EInputFlag inputFlag;

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

            openingMgr = new OpeningManager();
        }

        private static TaskType GetTaskGroup(TaskType taskType)
        {
            int temp = (int)taskType % 1000;
            return (TaskType)((int)taskType - temp);
        }
        public static void AddTask(TaskType taskType, TaskUpdateType taskUpdateType)
        {
            ContentTaskContainer task;
            bool isTaskAdded;

            TaskType taskGroupType = GetTaskGroup(taskType);
            switch (taskGroupType)
            {
                case TaskType.OPENGING:
                    isTaskAdded = openingMgr.TryAddTask(taskType, out task);
                    break;
                default:
                    Error.DebugAssert(ErrorCode.CANNOT_FIND_TASK_GROUP, taskGroupType.ToString());
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
                    Error.DebugAssert(ErrorCode.CANNOT_FIND_TASK_UPDATE_TYPE, taskUpdateType.ToString());
                    break;
            }
        }


        // MonoBehaviour
        private void Start()
        {
            AddTask(TaskType.OP_PLAY_OPENING, TaskUpdateType.UPDATE);
        }
        private void Update()
        {
            // update: input
            inputFlag = IDxInput.TryGetInput();

            // update: contents
            for (int i = 0; i < updates.Count(); ++i)
            {
                ContentTaskState state = updates[i].Run();

                switch (state)
                {
                    case ContentTaskState.SUCCESS:
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
        private void FixedUpdate()
        {
            // inputFlag는 Update()에서 받았음

            for (int i = 0; i < fixedUpdates.Count(); ++i)
            {
                ContentTaskState state = fixedUpdates[i].Run();

                switch (state)
                {
                    case ContentTaskState.SUCCESS:
                        // ...
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

