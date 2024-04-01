using System;
using System.Collections.Generic;
using UnityEngine;

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

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
namespace DevDataType
{
    [Serializable]
    public struct Tile_sample
    {
        private int key;

        //total 10 bytes
        private ushort move; // 16: flag
        private uint   info;   // 32: layer(2), status(6), link(24)
        private uint   height; // 27: flag

        public byte Layer { get => (byte)(info >> 30); }
        public byte Status { get => (byte)((info >> 24) & 0x3F); }
        public float Scale
        {
            get
            {
                float scale = 1f;
                if (0 != ((byte)(TileFeature.Small) & info))
                {
                    scale = 0.5f;
                }

                return PTile.GetScale(TileSize.Default, scale);
            }
        }

        public int Key { get => key; }
        public int Info { get => (int)info; }
        public int Move { get => move; }
        public int Height { get => (int)height; }
        public int Link { get => (int)(info & 0xFFFFFF); }

        public bool IsMovable(int quarant)
        {
            return 0 != (move & (1 << quarant));
        }
        public void GetHeightMask(int quarant, out int p0, out int p1)
        {
            p0 = -1; p1 = -1;
            switch (quarant)
            {
                case  0: p0 = 0; p1 = 1; break;
                case  4: p0 = 1; p1 = 2; break;
                case  5: p0 = 2; p1 = 5; break;
                case 13: p0 = 5; p1 = 8; break;
                case 14: p0 = 7; p1 = 8; break;
                case 10: p0 = 6; p1 = 7; break;
                case 11: p0 = 3; p1 = 6; break;
                case  3: p0 = 0; p1 = 3; break;
            }
#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
            Debug.Assert(-1 != p0 && -1 != p1);
#endif
            p0 = ((int)height >> p0 * 3) & 0b111;
            p1 = ((int)height >> p1 * 3) & 0b111;
        }

        public Tile_sample(int key)
        {
            this.key    = key;
            this.info   = 0;
            this.move   = 0;
            this.height = 0;
        }
        public Tile_sample(int key, int info, int move, int height)
        {
            this.key    = key;
            this.info   = (uint)info;
            this.move   = (ushort)move;
            this.height = (uint)height;
        }
    }
}

[Serializable]
public struct Tile_t
{
    //total: 9 bytes
    private byte info; // 2: mask_Layer, 6:flag_Status
    private ushort move; // 16: flag
    private ushort link; // 16: mask
    private uint height; // 27: mask

    public byte Layer { get => (byte)(info >> 6); }
    public byte Status { get => (byte)(info & 0x3F); }
    public float Size
    {
        get
        {
            float scale = 1f;
            if (0 != ((byte)(TileFeature.Small) & info))
            {
                scale = 0.5f;
            }

            return PTile.GetScale(TileSize.Default, scale);
        }
    }

    public int Info { get => info; }
    public int Move { get => move; }
    public int Link { get => link; }
    public int Height { get => (int)height; }

    public Tile_t(int info, int move, int height)
    {
        this.info = (byte)info;
        this.move = (ushort)move;
        this.height = (uint)height;
        this.link = 0;
    }
}
#endif