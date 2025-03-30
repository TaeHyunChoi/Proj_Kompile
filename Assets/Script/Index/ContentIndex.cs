
namespace Script.Index
{
    public enum IngameUpdateState
    {
        RUNNING,
        SUCCESS,
        FAILURE
    };
    public enum IngameHandlerType
    {
        NONE = 0,

        OPENING,
        ENTER_FIELD,

    }
    public enum IngameHandlerState
    {
        NONE = 0,

        RUNNING,
        SUCCESS,
        FAILURE
    }
}
