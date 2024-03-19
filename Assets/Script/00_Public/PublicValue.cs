using System;
using UnityEngine;

public static class Public
{
    public const float SPEED_FADE = 1.25f;
    public const float SPEED_MOVE = 4f;

    public const int    TILE_BIT_HEIGHT     = 4;
    public const float  TILE_SIZE           = 1f;
    public const float  TILE_INVERSE         = 1f    / TILE_SIZE;
    public const float  TILE_HALF           = 0.5f  * TILE_SIZE;
    public const float  TILE_HALF_INVERSE    = 1f    / TILE_HALF;
    public const float  TILE_QUATER         = 0.25f * TILE_SIZE;

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
public enum TileFeature
{ 
    None = 0,
    Inner
}


//Currently there was no need to use a separate namespace... but I wanted to try it.
namespace CDataStructure
{
    using static Public;

    [Serializable]
    public struct Tile_t
    {
        private int dataFlag;
        private int linkFlag;

        public int DataFlag { get => dataFlag; }

        public bool IsMovable(int quarant)
        {
            return 0 != (dataFlag & (1 << quarant));
        }
        public bool IsMovable(Vector3 point)
        {
            Vector3 diff = point - PVoxel.GetPivot(point);
            int quarant = PVoxel.GetMoveQuarant(diff);

            return IsMovable(quarant);
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
                case 0: relative += 18; break;
                case 1: /* y is same; */ break;
                case 2: relative += 9; break;
            }

            return 0 != (linkFlag & (1 << relative));
        }

        public int GetHeightCode(int index)
        {
            //Written with bit operations and binary notation instead of calculation formulas so that it can be read intuitively
            int value;
            switch (index)
            {
                case 0: value = (dataFlag >> TILE_BIT_HEIGHT) & 0b_11; break;
                case 1: value = (dataFlag >> TILE_BIT_HEIGHT) & 0b_11_00; break;
                case 2: value = (dataFlag >> TILE_BIT_HEIGHT) & 0b_11_00_00; break;
                case 3: value = (dataFlag >> TILE_BIT_HEIGHT) & 0b_11_00_00_00; break;
                case 4: value = (dataFlag >> TILE_BIT_HEIGHT) & 0b_11_00_00_00_00; break;
                default: return -1;
            }

            value >>= index * 2;
            return value;
        }
        public float GetYValue(int index)
        {
            int code = (dataFlag >> TILE_BIT_HEIGHT) & (0b11 << index * 2);
            code >>= (index * 2);

            return code * TILE_HALF;
        }

        public Tile_t(int data)
        {
            dataFlag = data;
            linkFlag = 0;
        }
        public Tile_t(int data, int link)
        {
            dataFlag = data;
            linkFlag = link;
        }
    }
}