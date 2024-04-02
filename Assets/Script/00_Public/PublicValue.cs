using System;
using CMathf;

public static class Public
{
    public const float SPEED_FADE = 1.25f;
    public const float SPEED_MOVE = 4f;

    public const int    TILE_SHIFT_HEIGHT   = 8;

    //public const float  TILE_SIZE           = 1f;
    //public const float  TILE_INVERSE        = 1f    / TILE_SIZE;
    //public const float  TILE_HALF           = 0.5f  * TILE_SIZE;
    //public const float  TILE_HALF_INVERSE   = 1f    / TILE_HALF;
    //public const float  TILE_QUATER         = 0.25f * TILE_SIZE;
    //public const float  TILE_QUATER_INVERSE = 1f    / TILE_QUATER;

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

namespace DataType
{
    [Serializable]
    public struct Tile_t
    {
        //total 10 bytes
        private uint   info;   // 32: status(6), link(24, 12*2)
        private ushort move;   // 16: flag
        private uint   height; // TODO: 27=>36 bits ·Î ¼öÁ¤

        public float Scale
        {
            get
            {
                float scale = 1f;
                if (0 != ((byte)(TileFeature.Small) & (info >> 24)))
                {
                    scale = 0.5f;
                }

                return PTile.GetScale(TileSize.Default, scale);
            }
        }
        public int Info { get => (int)info; }
        public TileFeature Status { get => (TileFeature)(info >> 24); }

        public int Move { get => move; }
        public int Height { get => (int)height; }
        public int Link { get => (int)(info & 0xFFFFFF); }

        public bool IsMovable(int quarant)
        {
            return 0 != (move & (1 << quarant));
        }
        public bool IsLinked(int indexLink)
        {
            uint mask = info >> (indexLink * 2);
            mask &= 0b11;
            return 0 != mask;
        }
        public float GetYValue(int key, int index)
        {
            float y = (key & 0x00_FF_00) >> 8;
            y = CMath.Floor(y * Scale, 2);

            float mask = (Height >> (index * 3)) & 0b111;
            mask = CMath.Floor(mask * PTile.GetScale(TileSize.Quater, Scale), 2);

            return y + mask;
        }

        public Tile_t(int info, int move, int height)
        {
            this.info   = (uint)info;
            this.move   = (ushort)move;
            this.height = (uint)height;
        }
    }
}