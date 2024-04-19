using System;
using System.Collections.Generic;
using CMathf;
using UnityEngine;

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
        public  const int DOWN_HOLD   = 1 << (DOWN + BIT_HOLD);
        public  const int UP_HOLD     = 1 << (UP + BIT_HOLD);
        public  const int LEFT_HOLD   = 1 << (LEFT + BIT_HOLD);
        public  const int RIGHT_HOLD  = 1 << (RIGHT + BIT_HOLD);
        public  const int ENTER_HOLD  = 1 << (ENTER + BIT_HOLD);
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
    public static class IDxTile
    {
        public const float SIZE = 1f;
        public const float SIZE_INVERSE = 1f / SIZE;
        public const float SIZE_HALF = 0.5f * SIZE;
        public const float SIZE_HALF_INVERSE = 1f / SIZE_HALF;
        public const float SIZE_QUATER = 0.25f * SIZE;
        public const float SIZE_QUATER_INVERSE = 1f / SIZE_QUATER;
        public const float SIZE_EIGHTH = 0.125f * SIZE;

        public const byte SHIFT_KEY_LAYER = 21; // 3bits, mask
        public const byte SHIFT_KEY_SCALE = 20; // 1bit,  flag
        public const byte SHIFT_KEY_X     = 12; // 8bits, mask
        public const byte SHIFT_KEY_Y     =  8; // 4bits, mask
        public const byte SHIFT_KEY_Z     =  0; // 8bits, mask

        public const byte SHIFT_TRIGGER_SCALE          = 14;
        public const byte SHIFT_TRIGGER_SCALE_VALUE    = 13;
        public const byte SHIFT_TRIGGER_LAYER          = 12;
        public const byte SHIFT_TRIGGER_LAYER_VALUE    =  8; 
        public const byte SHIFT_TRIGGER_INTERACT       =  7; 
        public const byte SHIFT_TRIGGER_INTERACT_VALUE =  0; 

        public const byte SHIFT_INFO_SCALE = 7;
    }
}
namespace DataType
{
    using static Index.IDxTile;

    [Serializable]
    public struct Tile_t
    {
        //total 10 bytes
        private byte   maskInfo;    //  8 bits: scale(1), status(7)
        private ushort maskTrigger; // 15 bits:  scale_trigger_flag(1),    scale_trigger_value(1),
                                    //           layer_trigger_flag(1),    layer_trigger_value(4),
                                    //           interact_trigger_flag(1), interact_trigger_value(7)
        private ulong  maskMove;    // 55 bits: height(13*3), move(16)

        public bool IsMovable(int indexTriangle)
        {
            int mask = (int)(maskMove & 0xFFFF);
            mask &= (1 << indexTriangle);
            return 0 != mask;
        }
        public bool HasTrigger(TileTrigger type, out int value)
        {
            value = 0;
            bool hasTrigger = (0 != (maskTrigger & (ushort)type));

            switch (type)
            {
                case TileTrigger.Scale:
                    value = (int)((maskTrigger >> SHIFT_TRIGGER_SCALE_VALUE) & 0b_0001);
                    break;
                case TileTrigger.Layer:
                    value = (int)((maskTrigger >> SHIFT_TRIGGER_LAYER_VALUE) & 0b_1111);
                    break;
                case TileTrigger.Interact:
                    value = (int)((maskTrigger >> SHIFT_TRIGGER_INTERACT_VALUE) & 0b_0011_1111_1111);
                    break;
            }

            return hasTrigger;
        }
        public float GetScale(TileSize type = TileSize.Default)
        {
            float scale = (0 != (maskInfo >> SHIFT_INFO_SCALE)) ? 0.5f : 1f;
            return TileUtility.GetScale(type, scale);
        }
        public float GetYValue(int keyMy, Vector3 point)
        {
            //point가 속한 분면을 구해서
            Vector3 pivot    = TileUtility.GetPivot(keyMy, GetScale());
            float scale_half = GetScale(TileSize.Half);
            int quarant      = TileUtility.GetTriangleIndex(point - pivot, scale_half);

            //각 포인트에 해당하는 높이 구해서...
            long height = (long)(maskMove >> 16);
            Vector3[] points = TileUtility.GetQuarantPoints(pivot, GetScale(), height, quarant);

            //평면의 방정식에 대입하면 y값을 구할 수 있다. (유의: 왼손 좌표계)
            Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]);
            normal.Normalize();
            normal = CMath.FloorToVector(normal, 3);
            float d = Vector3.Dot(normal, points[0]);

            return -(normal.x * point.x + normal.z * point.z - d) / normal.y;
        }

        //Only for Tile Map Sampling
#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
        public int Info { get => maskInfo; }
        public int Move { get => (int)(maskMove & 0xFFFF); }
        public long Height { get => (long)(maskMove >> 16); }
        public int Trigger { get => maskTrigger; }
        public Tile_t(int info, int trigger, int move, long height)
        {
            this.maskInfo    = (byte)info;
            this.maskTrigger = (ushort)trigger;
            this.maskMove    = (ulong)((height << 16) | (long)move);
        }
#endif
    }
}
namespace CMathf
{
    public static class CMath
    {
        public static float Floor(float value, int exponent)
        {
            float d = (int)Mathf.Pow(10, exponent);

            //exponent媛 ?遺遺?2 ?먮뒗 3?대?濡?罹먯떛
            float d_invert;
            switch (exponent)
            {
                case 2: d_invert = 0.01f; break;
                case 3: d_invert = 0.001f; break;
                default: d_invert = 1 / d; break;
            }

            return (int)(value * d) * d_invert;
        }
        public static int FloorToInt(float value, int exponent)
        {
            return (int)Floor(value, exponent);
        }
        public static Vector3 FloorToVector(Vector3 value, int exponent)
        {
            float x = Floor(value.x, exponent);
            float y = Floor(value.y, exponent);
            float z = Floor(value.z, exponent);

            return new Vector3(x, y, z);
        }
    }
}