namespace Kompile.Data
{
    using System;

    [Flags]
    public enum IDxInput
    {
        NONE = 0,

        LEFT = 1 << 1,
        RIGHT = 1 << 2,
        UP = 1 << 3,
        DOWN = 1 << 4,

        ENTER = 1 << 5,
        CANCEL = 1 << 6,
        ACTION = 1 << 7,

        MOVE_ALL = LEFT | RIGHT | UP | DOWN,
        SELECT_ALL = ENTER | CANCEL | ACTION
    }
}