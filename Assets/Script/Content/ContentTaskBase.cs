
namespace Script.ContentTask
{
    using Script.Interface;
    using Script.Index;

    public class ContentTask
    {
        private readonly IContentTaskUpdater[] tasks;
        private readonly int length;

        private int index;
        private ContentTaskState state;

        public ContentTask(params IContentTaskUpdater[] taskArray)
        {
            tasks   = taskArray;
            length  = taskArray.Length;
            state   = ContentTaskState.RUNNING;
        }

        public ContentTaskState Run(IDxInput.EInputFlag inputFlag)
        {
            state = tasks[index].MoveNext(inputFlag);

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

