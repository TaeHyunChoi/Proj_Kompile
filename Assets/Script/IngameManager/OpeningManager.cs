namespace Script.GameManager
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
                      // LOAD_TABLE를 비동기로 돌리고 있는게 좋지 않니?
                      // 상태값 체크를 OpeningManager에서 들고 있으면...?
                      // 개별 작업값을 OpeningManager에서 어떻게 관리?
                       new OP_PlayLogo(),
                       new OP_PlayDemo(),
                       new OP_PlayTitle(), //여기서 .AddTask(UI_TITLE)을 날리는거임
                     };
                    break;

                default:
                    Error.DebugAssert(ErrorCode.CANNOT_FIND_TASK, type.ToString());
                    return false;
            }

            contentTask = new ContentTaskContainer(type, tasks);
            return true;
        }
    }
}