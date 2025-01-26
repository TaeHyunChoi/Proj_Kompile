
namespace Script.Content
{
    using Script.Interface;
    using Script.Index;

    public abstract class ContentTaskBase
    {
        protected int index;

        public ContentTaskBase()
        {
            index = 0;
        }

        public abstract ContentTaskState MoveNext();
    }

    public class ContentTaskContainer
    {
        private readonly ContentTaskBase[] tasks;
        private readonly int length;

        private int index;
        private TaskType type;
        private ContentTaskState state;

        public ContentTaskContainer(TaskType taskType, ContentTaskBase[] taskArray)
        {
            tasks   = taskArray;
            length  = taskArray.Length;
            type    = taskType;
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

