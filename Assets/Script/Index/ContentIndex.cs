
namespace Script.Index
{
    public enum ContentTaskState
    {
        RUNNING,
        SUCCESS,
        FAILURE
    };

    public enum TaskType
    {
        UI = 0000,
        UI_TITLE_NEW,
        UI_TITLE_LOAD,
        UI_TITLE_OPTION,
        UI_TITLE_QUIT,

        OPENGING = 1000,
        OP_PLAY_OPENING,
        OP_OPEN_TITLE,
        OP_ENTER_GAME,

        FILED = 2000,
        // FD_

        BATTLE = 3000,
        // BT_

        IngameEVENT = 4000,
        // EV_

    }

    public enum TaskUpdateType
    { 
        NONE         = 0,
        UPDATE       = 1,
        FIXED_UPDATE = 2
    }
}
