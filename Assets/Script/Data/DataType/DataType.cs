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
            MOVE_ALL,

            ENTER,
            CANCEL,
            ACTION
        }
    }
}