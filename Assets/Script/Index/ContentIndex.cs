
namespace Script.Index
{
    public enum ETaskState
    {
        RUNNING,
        SUCCESS,
        FAILURE
    };

    public enum TaskType
    {
        UI = 0000,
        UI_TITLE_MENU_FADE,

        OPENGING = 1000,
        OP_PLAY_OPENING,
        OP_START_GAME,

        FILED = 2000,
        // FD_

        BATTLE = 3000,
        // BT_

        IngameEVENT = 4000,
        // EV_

    }

    public enum TaskUpdateType
    {
        NONE = 0,
        UPDATE,
        FIXED_UPDATE,
        LATE_UPDATE
    }
}
