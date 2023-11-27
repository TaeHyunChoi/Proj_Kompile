public static class DINDEX
{
    public const int DOWN   = 1 << 0;
    public const int UP     = 1 << 1;
    public const int LEFT   = 1 << 2;
    public const int RIGHT  = 1 << 3;
    public const int ENTER  = 1 << 4;
    public const int CANCEL = 1 << 5;
    public const int ESCAPE = 1 << 6;
    public const int ACTION = 1 << 7;
}

public enum GameLayer
{
    UI = 0,
    Field,
    Battle
}
