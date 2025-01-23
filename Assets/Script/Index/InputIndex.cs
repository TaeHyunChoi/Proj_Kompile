
namespace Script.Index
{
    using System;
    using UnityEngine;

    public static class IDxInput
    {
        private const int SHIFT_BIT_HOLD = 8;

        [Flags]
        public enum EInputFlag
        {
            NONE = 0,

            DOWN    = 1 << 0,
            UP      = 1 << 1,
            LEFT    = 1 << 2,
            RIGHT   = 1 << 3,
            ENTER   = 1 << 4,
            CANCEL  = 1 << 5,
            ESCAPE  = 1 << 6,
            ACTION  = 1 << 7,

            DOWN_HOLD   = 1 << (DOWN    + SHIFT_BIT_HOLD),
            UP_HOLD     = 1 << (UP      + SHIFT_BIT_HOLD),
            LEFT_HOLD   = 1 << (LEFT    + SHIFT_BIT_HOLD),
            RIGHT_HOLD  = 1 << (RIGHT   + SHIFT_BIT_HOLD),
            ENTER_HOLD  = 1 << (ENTER   + SHIFT_BIT_HOLD),
            CANCEL_HOLD = 1 << (CANCEL  + SHIFT_BIT_HOLD),
            ESCAPE_HOLD = 1 << (ESCAPE  + SHIFT_BIT_HOLD),
            ACTION_HOLD = 1 << (ACTION  + SHIFT_BIT_HOLD),
            MASK_HOLD   = 0x0F << SHIFT_BIT_HOLD,

            ALL = 0xFF
        }

        // 얘도 new input system으로 맞춰서 고치던가 그래야 함;
        public static bool TryGetInput(out EInputFlag inputFlag)
        {
            inputFlag = EInputFlag.NONE;

            //Button Down
            if (Input.GetButtonDown("DOWN"))    { inputFlag |= EInputFlag.DOWN; }
            if (Input.GetButtonDown("UP"))      { inputFlag |= EInputFlag.UP; }
            if (Input.GetButtonDown("LEFT"))    { inputFlag |= EInputFlag.LEFT; }
            if (Input.GetButtonDown("RIGHT"))   { inputFlag |= EInputFlag.RIGHT; }

            if (Input.GetButtonDown("ENTER"))   { inputFlag |= EInputFlag.ENTER; }
            if (Input.GetButtonDown("CANCEL"))  { inputFlag |= EInputFlag.CANCEL; }
            if (Input.GetButtonDown("ESCAPE"))  { inputFlag |= EInputFlag.ESCAPE; }
            if (Input.GetButtonDown("ACTION"))  { inputFlag |= EInputFlag.ACTION; }

            //Button Hold
            if (Input.GetButton("DOWN"))        { inputFlag |= EInputFlag.DOWN_HOLD; }
            if (Input.GetButton("UP"))          { inputFlag |= EInputFlag.UP_HOLD; }
            if (Input.GetButton("LEFT"))        { inputFlag |= EInputFlag.LEFT_HOLD; }
            if (Input.GetButton("RIGHT"))       { inputFlag |= EInputFlag.RIGHT_HOLD; }
            if (Input.GetButton("ACTION"))      { inputFlag |= EInputFlag.ACTION_HOLD; }

            return EInputFlag.NONE != inputFlag;
        }

        public static bool Contains(this EInputFlag input, EInputFlag compares)
        {
            return 0 != (input & compares);
        }
    }

    public enum ContentTaskState
    {
        RUNNING,
        SUCCESS,
        FAILURE
    };

    //public static class IDxTile
    //{
    //    public const float SIZE = 1f;
    //    public const float SIZE_INVERSE = 1f / SIZE;
    //    public const float SIZE_HALF = 0.5f * SIZE;
    //    public const float SIZE_HALF_INVERSE = 1f / SIZE_HALF;
    //    public const float SIZE_QUATER = 0.25f * SIZE;
    //    public const float SIZE_QUATER_INVERSE = 1f / SIZE_QUATER;
    //    public const float SIZE_EIGHTH = 0.125f * SIZE;

    //    public const byte SHIFT_KEY_LAYER = 21; // 3bits, mask
    //    public const byte SHIFT_KEY_SCALE = 20; // 1bit,  flag
    //    public const byte SHIFT_KEY_X = 12; // 8bits, mask
    //    public const byte SHIFT_KEY_Y = 8; // 4bits, mask
    //    public const byte SHIFT_KEY_Z = 0; // 8bits, mask

    //    public const byte SHIFT_TRIGGER_SCALE = 14;
    //    public const byte SHIFT_TRIGGER_SCALE_VALUE = 13;
    //    public const byte SHIFT_TRIGGER_LAYER = 12;
    //    public const byte SHIFT_TRIGGER_LAYER_VALUE = 8;
    //    public const byte SHIFT_TRIGGER_INTERACT = 7;
    //    public const byte SHIFT_TRIGGER_INTERACT_VALUE = 0;

    //    public const byte SHIFT_INFO_SCALE = 7;
    //}
}
