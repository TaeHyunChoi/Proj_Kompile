
namespace Script.Index
{
    public enum IngameUpdateState
    {
        RUNNING,
        SUCCESS,
        FAILURE
    };
    public enum IngameProcedureType
    {
        NONE = 0,
        
        OPENING,
        NEW_GAME,

        FIELD,
    }
    public enum IngameHandlerState
    {
        NONE = 0,

        RUNNING,
        SUCCESS,
        FAILURE
    }
}
