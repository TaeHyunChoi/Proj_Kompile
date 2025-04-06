public enum EAnimeCodeToString
{ 
    NONE,

    IDLE_FRONT,
    IDLE_BACK,
    IDLE_LEFT,
    IDLE_RIGHT,

    MOVE_FRONT,
    MOVE_BACK,
    MOVE_LEFT,
    MOVE_RIGHT,
}
public enum UpdaterType
{ 
    UPDATE          = 0,
    FIXED_UPDATE,
    LATE_UPDATE,

    INPUT
}
public enum IngameMessageType
{
    NONE,

    INPUT_CONTROL,

    GET_ASSET,
    END_OBJECT_PROCESS,
    SELECT_ITEM,
}