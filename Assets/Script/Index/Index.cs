namespace Script.Index
{
    public static class Index
    {
        // MAP : 이게 왜 이렇게 있는지 모르겠네;
        public const byte FIELD_HALF_LENGTH = 9;
        //public const byte GRID_X_LENGTH     = 16;
        //public const byte GRID_Y_LENGTH     = 8;
        //public const byte GRID_Z_LENGTH     = 16;

        public static int ToInt(this AssetCode assetName)
        {
            return (int)assetName;
        }
    }
}

