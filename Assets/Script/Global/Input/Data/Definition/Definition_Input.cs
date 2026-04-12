namespace Script.Input.Data
{
    using System;

    public static class Definition
    {
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

        public readonly struct InputState
        {
            private readonly IDxInput current;
            private readonly IDxInput previous;
            
            public InputState(IDxInput current, IDxInput previous)
            {
                this.current = current;
                this.previous = previous;
            }

            public bool IsDown(IDxInput input)
            {
                return (current & input) != 0
                    && (previous & input) == 0;
            }

            public bool IsPressing(IDxInput input)
            {
                return (current & input) != 0;
            }

            public bool IsUp(IDxInput input)
            {
                return (current & input) == 0 && (previous & input) != 0;
            }
        }
    }
}