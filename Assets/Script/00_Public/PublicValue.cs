using System;

public static class Public
{
    //save to config?
    public static readonly float FADE_SPEED = 1.25f;
    public static readonly float MOVE_SPEED = 4f;
    public static readonly float GRID_SIZE = 1f;
    public static readonly float GRID_SIZE_INVERT = 1f / 1f;
    public static readonly float HALF_GRID_SIZE = 1f * 0.5f;
    public static readonly float HALF_GRID_SIZE_INVERT = 1f / (1f * 0.5f);

    //For test Sampler 3rd)
    public const float VOXEL_SIZE          = 1f;
    public const float VOXEL_INVERT        = 1f;
    public const float VOXEL_HALF_SIZE     = 0.5f;
    public const float VOXEL_HALF_INVERT   = 2f;

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
public enum VoxelType : int
{
    None = 0,

    Plain,
    Slope45,
    Obstacle,
}



// struct

[Serializable]
public struct Voxel_t
{
    public Voxel_t(int sub)
    {
        this.sub = sub;
    }
    private int sub;
    public int SubVoxel { get => sub; }
    public VoxelType GetSubType(int quadrant)
    {
        int typed = sub & (0b11 << quadrant * 2);
        return (VoxelType)(typed >> (quadrant * 2));
    }
}

namespace PublicValue
{
    public struct Voxel_t3
    {
        private const int BITSHIFT_INCLINE = 4 * 2;  //use 2 bits (0,30,45,90)
        private const int BITSHIFT_OBJECT = 4 * 3;   // normal.y (None, Plain, Obstacle, Trigger?)

        private int data;

        public int Data { get => data; }
        public int Incline
        {
            get
            {
                int inc = data & (0b11 << BITSHIFT_INCLINE);
                inc >>= BITSHIFT_INCLINE;

                switch (inc)
                {
                    case 1: return 30;
                    case 2: return 45;
                    case 3: return 90;
                }

                return 0;
            }
        }
        public VoxelType ObjectType
        {
            get
            {
                int type = data & (0b11 << BITSHIFT_OBJECT);
                type >>= BITSHIFT_OBJECT;

                return (VoxelType)type;
            }
        }
        public int Move
        {
            get
            {
                return data & 0xFF;
            }
        }


        public bool IsMovable(int idxSub)
        {
            return (data & (1 << idxSub)) != 0;
        }

        public Voxel_t3(int data)
        {
            this.data = data;
        }
        public Voxel_t3(VoxelType objType, int incline, int move)
        {
            data = (int)objType << BITSHIFT_OBJECT | incline << BITSHIFT_INCLINE | move;
        }
    }
    public struct Voxel_t4
    {
        // [8:__] [16:slope] [8:movable]
        private const int BITSHIFT_Degree = 8;
        private int data;

        //Degree
        public int Degree { get => data & 0xFFFF00; }
        public int Move   { get => data & 0x0000FF; }
        public int DegToBit(int degreeInt)
        {
            switch (degreeInt)
            {
                case 30: return 1;
                case 45: return 2;
                case 90: return 3;
            }

            return 0;
        }
        public int BitToDeg(int idxSub)
        {
            int shift = (idxSub * 2) + BITSHIFT_Degree;
            int inc = data & (0b11 << shift);

            switch (inc >>= shift)
            {
                case 1: return 30;
                case 2: return 45;
                case 3: return 90;
            }

            return 0;
        }
        public int GetDegreeMask(int idxSub, int degreeInt)
        {
            int shift = (idxSub * 2) + BITSHIFT_Degree;

            int degMask = Degree & ~(0b11 << shift);
            return degMask |= DegToBit(degreeInt) << shift;
        }

        //Move
        public int GetMoveMask(int idxSub, int isMove)
        {
            if (isMove == 0)
            {
                return Move & ~(1 << idxSub);
            }
            else
            {
                return Move | (1 << idxSub);
            }
        }
        public bool IsMovable(int idxSub)
        {
            return (Move & (1 << idxSub)) != 0;
        }

        public Voxel_t4(int data)
        {
            this.data = data;
        }
        public Voxel_t4(int sub, int degreeInt, int moveMask)
        {
            int degreeMask;
            int shift = BITSHIFT_Degree + sub * 2;

            switch (degreeInt)
            {
                case 90: degreeMask = 0b11 << shift; break;
                case 45: degreeMask = 0b10 << shift; break;
                case 30: degreeMask = 0b01 << shift; break;
                default: degreeMask = 0b00 << shift; break;
            }

            data = degreeMask | moveMask;
        }
    }
}