namespace Script.Manager
{
    using Script.Index;
    using Script.Content;

    public class OpeningManager
    {
        public bool TryAddTask(TaskType type, out ContentTaskContainer contentTask)
        {
            ContentTaskBase[] tasks;
            contentTask = null;

            switch (type)
            {
                case TaskType.OP_PLAY_OPENING:
                    tasks = new ContentTaskBase[] 
                    {
                       new OP_PlayLogo(),
                       new OP_PlayDemo(),
                       new OP_PlayTitle(), //여기서 .AddTask(UI_TITLE)을 날리는거임
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