namespace Script.Manager
{
    using Script.Index;
    using Script.Content;
    using Script.Interface;

    [System.Obsolete]
    public class x_OpeningManager
    {
        public bool TryAddTask(UpdaterIndex type, out ContentTaskContainer contentTask)
        {
            ITaskUpdater[] tasks;
            contentTask = null;

            switch (type)
            {
                case UpdaterIndex.OP_PLAY_OPENING:
                    tasks = new ITaskUpdater[]
                    {
                       new OP_PlayTitleAnime(),
                       new UI_TitleMenu()
                     };
                    break;

                case UpdaterIndex.OP_START_GAME:
                    tasks = new ITaskUpdater[]
                    {
                        new UI_LoadingCurtain(true),
                        // OP_CloseTitle
                        // FD_Initialize
                        new UI_LoadingCurtain(false),
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