namespace Script.Content
{
    using Script.Index;
    using Script.Interface;

    public class ContentTaskContainer
    {
        private readonly IIngameUpdater[] tasks;
        private readonly int length;

        public readonly UpdaterIndex Type;

        private UpdaterState state;
        private int index;

        public ContentTaskContainer(UpdaterIndex taskType, IIngameUpdater[] taskArray)
        {
            tasks   = taskArray;
            length  = taskArray.Length;
            Type    = taskType;
            state   = UpdaterState.RUNNING;
        }

        public UpdaterState Run()
        {
            state = tasks[index].UpdateState();

            // 모든 작업 완료했는지 확인 > 안 끝났으면 다음으로 넘겨서 RUNNING
            if (UpdaterState.SUCCESS == state
                && ++index < length)
            {
                state = UpdaterState.RUNNING;
            }

            // 반환 받는 쪽(IngameManager)에서 상황 판단
            return state;
        }
    }
}

