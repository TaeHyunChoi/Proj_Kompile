using UnityEngine;

public static class EditMapConsts
{
    public const  int HEIGHT_MASK     = 0b_1111;
    public const  int HEIGHT_BITS     = 4;
    
    [System.Flags]
    public enum EditMapTileDirFlag
    {
        NONE    = 0,
        UP      = 1 << 0,
        DOWN    = 1 << 1,
        LEFT    = 1 << 2,
        RIGHT   = 1 << 3
    }
}
