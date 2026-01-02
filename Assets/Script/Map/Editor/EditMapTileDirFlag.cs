namespace Script.Data
{
#if UNITY_EDITOR
    [System.Flags]
    public enum EditMapTileDirFlag
    {
        NONE    = 0,
        UP      = 1 << 0,
        DOWN    = 1 << 1,
        LEFT    = 1 << 2,
        RIGHT   = 1 << 3
    }
#endif
}