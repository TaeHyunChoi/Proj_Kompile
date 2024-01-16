using System;

public static class Public
{
    //욕심 낸다면 이걸 에디터에서 실시간 조정하도록 만들고 싶은데...
    public static float FADE_SPEED = 1.25f;
    public static float MOVE_SPEED = 4f;
    public static float VOXEL_SIZE_INVERT = 1f / 0.25f;
    public static float VOXEL_SIZE = 0.25f;

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
    None   = -1,
    Option =  0,
    Title,
    SaveData,

    Count,
}
public enum VoxelType : int
{
    None = 0,
    Movable,
    Obstacle,
}

[Serializable]
public struct Voxel_t
{
    public Voxel_t(VoxelType type, int sub)
    {
        data = (int)type << 16 | sub;
    }
    private int data;
    public VoxelType Type { get => (VoxelType)(data >> 16); }
    public int Sub  { get => data & 0xFFFF; }
}