namespace Script.Content
{
    using Script.Index;
    using Script.Interface;

    public class ContentTaskContainer
    {
        private readonly IContentTaskUpdater[] tasks;
        private readonly int length;

        public readonly TaskType Type;

        private ContentTaskState state;
        private int index;

        public ContentTaskContainer(TaskType taskType, IContentTaskUpdater[] taskArray)
        {
            tasks   = taskArray;
            length  = taskArray.Length;
            Type    = taskType;
            state   = ContentTaskState.RUNNING;
        }

        public ContentTaskState Run()
        {
            state = tasks[index].MoveNext();

            // 모든 작업 완료했는지 확인 > 안 끝났으면 다음으로 넘겨서 RUNNING
            if (ContentTaskState.SUCCESS == state
                && ++index < length)
            {
                state = ContentTaskState.RUNNING;
            }

            // 반환 받는 쪽(IngameManager)에서 상황 판단
            return state;
        }
    }
}

