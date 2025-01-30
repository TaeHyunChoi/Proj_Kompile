namespace Script.Manager
{
    using Script.Index;
    using Script.Content;
    using Script.Interface;

    public class OpeningManager
    {
        public bool TryAddTask(TaskType type, out ContentTaskContainer contentTask)
        {
            ITaskUpdater[] tasks;
            contentTask = null;

            switch (type)
            {
                case TaskType.OP_PLAY_OPENING:
                    tasks = new ITaskUpdater[]
                    {
                       new OP_PlayTitleAnime(),
                       new UI_TitleMenu()
                     };
                    break;

                default:
#if UNITY_EDITOR || TEST_BUILD
                    OnlyDev.DevError.DebugAssert(ErrorCode.CANNOT_FIND_TASK, type.ToString());
#endif
                    return false;
            }

            contentTask = new ContentTaskContainer(type, tasks);
            return true;
        }
    }
}