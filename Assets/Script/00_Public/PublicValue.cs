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


namespace Index
{
    public static class IDxTile
    {
        public const float SIZE = 1f;
        public const float SIZE_INVERSE = 1f / SIZE;
        public const float SIZE_HALF = 0.5f * SIZE;
        public const float SIZE_HALF_INVERSE = 1f / SIZE_HALF;
        public const float SIZE_QUATER = 0.25f * SIZE;
        public const float SIZE_QUATER_INVERSE = 1f / SIZE_QUATER;
        public const float SIZE_EIGHTH = 0.125f * SIZE;

        public const byte SHIFT_KEY_LAYER = 30;
        public const byte SHIFT_KEY_SCALE = 20;
        public const byte SHIFT_KEY_X = 12;
        public const byte SHIFT_KEY_Y = 8;
        public const byte SHIFT_KEY_Z = 0;

        public const byte SHIFT_TRIGGER_SCALE          = 29;
        public const byte SHIFT_TRIGGER_SCALE_VALUE    = 28;
        public const byte SHIFT_TRIGGER_LAYER          = 27;
        public const byte SHIFT_TRIGGER_LAYER_VALUE    = 23;
        public const byte SHIFT_TRIGGER_INTERACT       = 22;
        public const byte SHIFT_TRIGGER_INTERACT_VALUE = 12;

        public const byte SHIFT_INFO_LINK  = 0;
        public const byte SHIFT_INFO_SCALE = 30;
    }
}
namespace DataType
{
    using static Index.IDxTile;

    [Serializable]
    public struct Tile_t
    {
        //total 12 bytes
        private uint  info;     // 22: scale(1), trigger(3), trigger_value(6), link(12)
        private ulong movement; // 55: height(13*3), move(16)

        public int  Link   { get => (int)(info & 0xFFFFFF);   }
        public int  Move   { get => (int)(movement & 0xFFFF); }
        public long Height { get => (long)(movement >> 16);   }

        public bool HasTrigger(TileTrigger type, out int value)
        {
            value = 0;
            bool hasTrigger = (0 != (info & (int)type));

            switch (type)
            {
                case TileTrigger.ScaleDown:
                    value = (int)((info >> SHIFT_TRIGGER_SCALE_VALUE) & 0b_0001);
                    break;
                case TileTrigger.Layer:
                    value = (int)((info >> SHIFT_TRIGGER_LAYER_VALUE) & 0b_1111);
                    break;
                case TileTrigger.Interact:
                    value = (int)((info >> SHIFT_TRIGGER_INTERACT_VALUE) & 0b_0011_1111_1111);
                    break;
            }

            return hasTrigger;
        }
        public float GetScale(TileSize type = TileSize.Default)
        {
            float scale = (0 != (info >> SHIFT_INFO_SCALE)) ? 0.5f : 1f;
            return TileUtility.GetScale(type, scale);
        }
        public bool IsLinked(int keyMy, Vector3 pointTarget)
        {
            float scale = GetScale();
            Vector3 pivot = TileUtility.GetPivot(keyMy, scale);
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
            switch (x | z)
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
                case 0b000_000: return true;
                default:        return false;
            }

            return 0 != (Link & (1 << index));
        }
        public float GetYValue(int keyMy, Vector3 point)
        {
            //point가 속한 분면을 구해서
            Vector3 pivot    = TileUtility.GetPivot(keyMy, GetScale());
            float scale_half = GetScale(TileSize.Half);
            int quarant      = TileUtility.GetTriangleIndex(point - pivot, scale_half);

            //각 포인트에 해당하는 높이 구해서...
            Vector3[] points = TileUtility.GetQuarantPoints(pivot, GetScale(), Height, quarant);

            //평면의 방정식에 대입하면 y값을 구할 수 있다. (유의: 왼손 좌표계)
            Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]);
            normal.Normalize();
            normal = CMath.FloorToVector(normal, 3);
            float d = Vector3.Dot(normal, points[0]);

            return -(normal.x * point.x + normal.z * point.z - d) / normal.y;
        }

        //Tile Map Sampling에서만 사용
#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
        public int Info { get => (int)info; }
        public long Movement { get => (long)movement; }
        public bool IsMovable(int indexTriangle)
        {
            return 0 != (Move & (1 << indexTriangle));
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
        public Tile_t(int info, long movement)
        {
            this.info = (uint)info;
            this.movement = (ulong)movement;
        }
        public Tile_t(int info, int move, long height)
        {
            this.info = (uint)info;
            this.movement = (ulong)((height << 16) | (long)move);
        }
        public void DebugLog(int key)
        {
            string trigger = string.Empty;

            if (true == HasTrigger(TileTrigger.ScaleDown, out int not_used))
            {
                trigger += "Scale Down, ";
            }
            if (true == HasTrigger(TileTrigger.Layer, out not_used))
            {
                trigger += "Layer, ";
            }
            if (true == HasTrigger(TileTrigger.Interact, out not_used))
            {
                trigger += "Interact, ";
            }

            string move = System.Convert.ToString(Move, 2).ToString();
            string height = System.Convert.ToString(Height, 2).ToString();
            string link = System.Convert.ToString(Info & 0xFFF, 2);
            float scale = GetScale(TileSize.Default);
            Debug.Log($"{key}:{TileUtility.GetPivot(key, scale):F3}(scale:{scale}, trigger:{trigger}) m:{move} l:{link}\nh:{height}");
        }
#endif
    }
}
