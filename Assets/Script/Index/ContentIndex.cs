
namespace Script.Index
{
    public enum UpdaterState
    {
        RUNNING,
        SUCCESS,
        FAILURE
    };

    // 얘도 애매하네...?
    public enum UpdaterIndex
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

    public enum IngameLogicIndex
    {
        NONE = 0,

        OPENING,
    }
    public enum IngameState
    {
        NONE = 0,

        RUNNING,
        SUCCESS,
        FAILURE
    }
}
