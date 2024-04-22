using System;
using CMathf;
using UnityEngine;

// enum
public enum ESceneState : byte
{ 
    None,
    Load,
    Play,
    Pause,
    Leave,
}
public enum EStat
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
public enum EAssetType : byte
{ 
    None     = 0,
    AnimCtrl = 1,
}
public enum EAnimeCodeToString
{ 
    NONE,

    IDLE_FRONT,
    IDLE_BACK,
    IDLE_LEFT,
    IDLE_RIGHT,

    MOVE_FRONT,
    MOVE_BACK,
    MOVE_LEFT,
    MOVE_RIGHT,
} //해당 자료형 값에 .ToString()하겠다는 뜻으로 접미사 ToString을 붙임

[Flags]
public enum EGameStateFlag : byte
{ 
    None    = 0,
    Opening = 1 << 4,
    Field   = 1 << 5,
    Battle  = 1 << 6,
    Event   = 1 << 7
}
public enum EUIType : byte
{
    Title = 0,
}

namespace Index
{
    public static class IDxInput
    {
        private const int BIT_HOLD = 8;

        //조금이라도 메모리를 연속적으로 사용하기 위하여 Enum으로 처리
        [Flags]
        public enum EInput
        {
            NONE    = 0,

            DOWN    = 1 << 0,
            UP      = 1 << 1,
            LEFT    = 1 << 2,
            RIGHT   = 1 << 3,
            ENTER   = 1 << 4,
            CANCEL  = 1 << 5,
            ESCAPE  = 1 << 6,
            ACTION  = 1 << 7,

            DOWN_HOLD   = 1 << (DOWN   + BIT_HOLD),
            UP_HOLD     = 1 << (UP     + BIT_HOLD),
            LEFT_HOLD   = 1 << (LEFT   + BIT_HOLD),
            RIGHT_HOLD  = 1 << (RIGHT  + BIT_HOLD),
            ENTER_HOLD  = 1 << (ENTER  + BIT_HOLD),
            CANCEL_HOLD = 1 << (CANCEL + BIT_HOLD),
            ESCAPE_HOLD = 1 << (ESCAPE + BIT_HOLD),
            ACTION_HOLD = 1 << (ACTION + BIT_HOLD),
            MASK_HOLD   = 0x0F << BIT_HOLD,

            ALL         = 0xFF
        }

        public static bool Compare(EInput input, EInput compare)
        {
            return (input & compare) != 0;
        }
        public static bool Compare(EInput input, params EInput[] compares)
        {
            for (int i = 0; i < compares.Length; ++i)
            {
                if (0 != (input & compares[i]))
                {
                    return true;
                }
            }

            return false;
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
        private byte   maskInfo;    //  8 bits: scale(1), state(7)
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

        // struct Tile_t
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
                case TileTrigger.Event:
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

        //strut Tile_t
        public float GetYValue(int keyMy, Vector3 point)
        {
            //point가 속한 삼각형 인덱스를 구한다.
            Vector3 pivot    = TileUtility.GetPivot(keyMy, GetScale());
            float scale_half = GetScale(TileSize.Half);
            int triangle      = TileUtility.GetTriangleIndex(point - pivot, scale_half);

            //각 포인트에 해당하는 높이 구한다. (배열을 사용하지 않기 위해 out 3번 사용...)
            //[주의!] 유니티는 "왼손 좌표계"이므로 외적 계산을 반대로 생각해야 한다...
            long height = (long)(maskMove >> 16);
            TileUtility.GetTrianglePoints(pivot, GetScale(), height, triangle, out Vector3 p0, out Vector3 p1, out Vector3 p2);

            //평면의 방정식에 대입하면 y값을 구할 수 있다.
            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);
            normal.Normalize();
            normal = CMath.FloorToVector(normal, 3);
            float d = Vector3.Dot(normal, p0);

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