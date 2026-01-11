namespace Script.Data
{
    using System;

    public static class DataType
    {
        [Flags]
        public enum IDxInput
        {
            NONE = 0,
            
            LEFT,
            RIGHT,
            UP,
            DOWN,


            ENTER,
            CANCEL,
            ACTION,
            
            MOVE_ALL = LEFT | RIGHT | UP | DOWN,
            SELECT_ALL = ENTER | CANCEL | ACTION
        }

        public readonly struct InputState
        {
            private readonly IDxInput current;
            private readonly IDxInput previous;

            #if UNITY_EDITOR
            public IDxInput Curr => current;
            public IDxInput Prev => previous; 
            #endif
            
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