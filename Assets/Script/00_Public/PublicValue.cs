using System;
using UnityEngine;

public static class Public
{
    //save to config?
    public static readonly float FADE_SPEED = 1.25f;
    public static readonly float MOVE_SPEED = 4f;
    //public static readonly float GRID_SIZE = 1f;
    //public static readonly float GRID_SIZE_INVERT = 1f / 1f;
    //public static readonly float HALF_GRID_SIZE = 1f * 0.5f;
    //public static readonly float HALF_GRID_SIZE_INVERT = 1f / (1f * 0.5f);

    public const int SHIFT_SLOPE_DIRECTION   =  8;  // 4 bits
    public const int SHIFT_SUB              =  0;  // 8 bits

    public const int OBSTACLE = 0b_00;
    public const int PLAIN = 0b_01;
    public const int SLOPE30 = 0b_10;
    public const int SLOPE45 = 0b_11;
    public const int SLOPE = 0b_10;

    public const int DEG_00 = 0b_00;
    public const int DEG_30 = 0b_01;
    public const int DEG_45 = 0b_10;
    public const int DEG_57 = 0b_11;

    //public const int VOXEL_BIT_MOVE =  0;
    public const int VOXEL_BIT_HEIGHT =  4;
    public const int VOXEL_BIT_DEG    =  8;
    public const int VOXEL_BIT_DIR    = 12;
    public const int VOXEL_BIT_TYPE   = 16;


    public const float VOXEL_SIZE          = 1f;
    public const float VOXEL_INVERT        = 1f / VOXEL_SIZE;
    public const float VOXEL_HALF_SIZE     = 0.5f * VOXEL_SIZE;
    public const float VOXEL_HALF_INVERT   = 1f / VOXEL_HALF_SIZE;

    public static void BlockInput(int input) { ;}
}
public static class IDxInput
{
    public const int DOWN   = 1 << 0;
    public const int UP     = 1 << 1;
    public const int LEFT   = 1 << 2;
    public const int RIGHT  = 1 << 3;
    public const int ENTER  = 1 << 4;
    public const int CANCEL = 1 << 5;
    public const int ESCAPE = 1 << 6;
    public const int ACTION = 1 << 7;

    private const int BIT_HOLD    = 8;
    public  const int DOWN_HOLD   = 1 << (DOWN   + BIT_HOLD);
    public  const int UP_HOLD     = 1 << (UP     + BIT_HOLD);
    public  const int LEFT_HOLD   = 1 << (LEFT   + BIT_HOLD);
    public  const int RIGHT_HOLD  = 1 << (RIGHT  + BIT_HOLD);
    public  const int ENTER_HOLD  = 1 << (ENTER  + BIT_HOLD);
    public  const int CANCEL_HOLD = 1 << (CANCEL + BIT_HOLD);
    public  const int ESCAPE_HOLD = 1 << (ESCAPE + BIT_HOLD);
    public  const int ACTION_HOLD = 1 << (ACTION + BIT_HOLD);
    public  const int MASK_HOLD   = 0x0F << BIT_HOLD;

    public const int ALL = 0xFF;

    public static bool Compare(int input, int compare)
    {
        return (input & compare) != 0;
    }
    public static bool Compare(int input, params int[] compares)
    {
        for (int i = 0; i < compares.Length; ++i)
        {
            if ((input & compares[i]) != 0)
            {
                return true;
            }
        }

        return false;
    }
    public static bool AnyKeyHold(int input)
    {
        return (input & MASK_HOLD) > 0;
    }
}

public delegate void InputDele(int input);

// enum
public enum Stat
{ 
    HP = 0,
    MP,
    EXP,
    STR,
    CON,
    INT,
    WIS,
    DEX,
    AGI,
    CHA,
    LUK,
    CNT
}
public enum ContentType
{
    None    = -1,
    Opening =  0,
    Field,
    Battle,
    Event,
    Count,
}
public enum UIType
{ 
    None   = 0,
    Option,
    Title,
    SaveData,

    Count,
}
public enum InteractType
{ 
    None = 0,
    Door,
    Talk,
}

//Currently there was no need to use a separate namespace... but I wanted to try it.
namespace CDataStructure
{
    using static Public;

    [Serializable]
    public struct Voxel_t
    {
        private int dataFlag;
        private int linkFlag;

        public int DataFlag   { get => dataFlag; }
        public int HeightFlag { get => dataFlag >> 4; }
        public int LinkFlag   { get => linkFlag; }

        public bool CanMoveTo(Vector3 point)
        {
            Vector3 diff = point - PVoxel.GetPivot(point);
            int quarant = PVoxel.GetMoveQuarant(diff);

            return IsMovable(quarant);
        }
        public bool IsMovable(int quarant)
        {
            return 0 != (dataFlag & (1 << quarant));
        }
        public int GetHeightCode(int index)
        {
            //Written with bit operations and binary notation instead of calculation formulas so that it can be read intuitively
            int value;
            switch (index)
            {
                case 0: value = (dataFlag >> VOXEL_BIT_HEIGHT) & 0b_11;             break;
                case 1: value = (dataFlag >> VOXEL_BIT_HEIGHT) & 0b_11_00;          break;
                case 2: value = (dataFlag >> VOXEL_BIT_HEIGHT) & 0b_11_00_00;       break;
                case 3: value = (dataFlag >> VOXEL_BIT_HEIGHT) & 0b_11_00_00_00;    break;
                case 4: value = (dataFlag >> VOXEL_BIT_HEIGHT) & 0b_11_00_00_00_00; break;
                default: return -1;
            }

            value >>= index * 2;
            return value;
        }
        public float GetYValue(int index)
        {
            int code = (dataFlag >> VOXEL_BIT_HEIGHT) & (0b11 << index * 2);
            code >>= (index * 2);

            return code * VOXEL_HALF_SIZE;
        }
        public bool IsLinkedWith(int fromKey, int toKey)
        {
            if (fromKey == toKey)
            {
                return true;
            }

            int flag = 0b_00_00_00;
            int mask = 0xFF;
            int nowMask, targetMask;

            for (int i = 2; i >= 0; --i)
            {
                nowMask = fromKey & (mask << 8 * i);
                targetMask = toKey & (mask << 8 * i);

                if (nowMask == targetMask)
                {
                    flag |= 0b_01 << (2 * i);
                }
                else if (nowMask > targetMask)
                {
                    flag |= 0b_10 << (2 * i);
                }
            }

            int relative = (flag >> 4) + 3 * (flag & 0b_11);
            flag &= 0b_00_11_00;
            switch (flag >> 2)
            {
                case 0: relative += 18;  break;
                case 1: /* y is same; */ break;
                case 2: relative += 9;   break;
            }

            return 0 != (linkFlag & (1 << relative));
        }


        public Voxel_t(int data)
        {
            dataFlag = data;
            linkFlag = 0;
        }
        public Voxel_t(int data, int link)
        {
            dataFlag = data;
            linkFlag = link;
        }
    }
}