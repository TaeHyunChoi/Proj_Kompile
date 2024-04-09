using System;
using System.Collections.Generic;
using CMathf;
using UnityEngine;

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

// enum
public enum SceneState : byte
{ 
    None,
    Load,
    Play,
    Pause,
    Leave,
}
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

[Flags]
public enum GameState : byte
{ 
    None    = 0,
    Opening = 1 << 4,
    Field   = 1 << 5,
    Battle  = 1 << 6,
    Event   = 1 << 7
}
public enum UIType : byte
{
    Title = 0,
}


namespace DataType
{
    [Serializable]
    public struct Tile_t
    {
        //total 12 bytes
        private uint  info;     // 32: status(6), link(24, 12*2)
        private ulong movement; // 55: height(13*3), move(16)

        public int Info { get => (int)info; }
        public long Movement { get => (long)movement; }

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
        public TileFeature Status { get => (TileFeature)(info >> 24); }
        public  int Link   { get => (int)(info & 0xFFFFFF);   }
        public  int Move   { get => (int)(movement & 0xFFFF); }
        public long Height { get => (long)(movement >> 16);   }

        public float GetScale(TileSize type)
        {
            float scale = 1f;
            if (0 != ((byte)(TileFeature.Small) & (info >> 24)))
            {
                scale = 0.5f;
            }

            return PTile.GetScale(type, scale);
        }
        public bool IsMovable(int keyMy, Vector3 point)
        {
            float   scale = Scale;
            Vector3 pivot = PTile.GetPivot(keyMy, scale);
            int     flag  = 1 << PTile.GetQuarant(point - pivot, GetScale(TileSize.Half));

            return 0 != (flag & Move);
        }
        public bool IsLinked(int keyMy, Vector3 pointTarget)
        {
            Vector3 pivot = PTile.GetPivot(keyMy, Scale);
            Vector3 diff = pointTarget - pivot;

            float half_size_inverse = GetScale(TileSize.Half_inverse);
            int x;
            if (diff.x < 0) { x = 0b111; }
            else            { x = CMath.FloorToInt(diff.x * half_size_inverse, 2); }
            x <<= 3;

            int z;
            if (diff.z < 0) { z = 0b111; }
            else            { z = CMath.FloorToInt(diff.z * half_size_inverse, 2); }

            int index;
            switch (x + z)
            {
                case 0b111_111: index =  0; break;
                case 0b111_000: index = 11; break;
                case 0b111_001: index = 10; break;
                case 0b111_010: index =  9; break;
                case 0b000_111: index =  1; break;
                case 0b001_111: index =  2; break;
                case 0b010_111: index =  3; break;
                case 0b010_000: index =  4; break;
                case 0b010_001: index =  5; break;
                case 0b010_010: index =  6; break;
                case 0b001_010: index =  7; break;
                case 0b000_010: index =  8; break;
                default: return false;
            }

            return 0 != (Link & (0b11 << index * 2));
        }
        public float GetYValue(int keyMy, Vector3 point)
        {
            //point가 속한 분면을 구해서
            Vector3 pivot    = PTile.GetPivot(keyMy, GetScale(TileSize.Default));
            float scale_half = GetScale(TileSize.Half);
            int quarant      = PTile.GetQuarant(point - pivot, scale_half);

            //각 포인트에 해당하는 높이 구해서...
            Vector3[] points = PTile.GetQuarantPoints(pivot, Scale, Height, quarant);

            //평면의 방정식에 대입하면 y값을 구할 수 있다. (유의: 왼손 좌표계)
            Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]);
            normal.Normalize();
            normal = CMath.FloorToVector(normal, 3);
            float d = Vector3.Dot(normal, points[0]);

            return - (normal.x * point.x + normal.z * point.z - d) / normal.y;
        }
#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
        //Tile Map Sampling에서만 사용
        public bool IsMovable(int quarant)
        {
            return 0 != (Move & (1 << quarant));
        }
        public int GetTriangleHeightMask(int triangle, int y)
        {
            int h0, h1;
            int mask;
            int shift;

            switch (triangle)
            {
                case 0: h0 = 0; h1 = 1; break;
                case 10: h0 = 6; h1 = 7; break;

                case 4: h0 = 1; h1 = 2; break;
                case 14: h0 = 7; h1 = 8; break;

                case 3: h0 = 0; h1 = 3; break;
                case 5: h0 = 2; h1 = 5; break;

                case 11: h0 = 3; h1 = 6; break;
                case 13: h0 = 5; h1 = 8; break;

                default: return -1;
            }

            shift = h0 * 3;
            mask = (int)((Height >> shift) & 0b111);
            h0 = (mask + y) << 3;

            shift = h1 * 3;
            mask = (int)((Height >> shift) & 0b111);
            h1 = mask + y;

            return h0 | h1;
        }
#endif

        public Tile_t(int info, long movement)
        {
            this.info = (uint)info;
            this.movement = (ulong)movement;
        }
        public Tile_t(int info, int move, long height)
        {
            this.info   = (uint)info;
            this.movement = (ulong)((height << 16) | (long)move);
        }
    }
}
