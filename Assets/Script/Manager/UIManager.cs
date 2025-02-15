namespace Script.Manager
{
    using Script.Index;
    using Script.Content;
    using Script.Interface;

    public class UIManager
    {
        public bool TryAddTask(UpdaterIndex type, out ContentTaskContainer contentTask)
        {
            IIngameUpdater[] tasks;
            contentTask = null;

            switch (type)
            {
                case UpdaterIndex.UI_TITLE_MENU_FADE:
                    tasks = new IIngameUpdater[]
                    {
                        // UI_TITLE_ 거기서 받아서 여차저차인건가?
                        // 오브젝트를 어떻게 제어할 것인가? 이걸 ITask에 넣는게 좋을까? 고민 필요
                        // 아이고 설계 어렵네~
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
