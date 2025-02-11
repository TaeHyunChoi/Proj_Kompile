namespace Script.Index
{
    using System;

    public enum CanvasType
    { 
        NONE = 0,
        CAMERA,
        OVERLAY
    }
    public enum EAssetName
    {
        NONE = 0,

        UnitBase,
        AnimCtrl_Ataho,
        AnimCtrl_Linxhang,
        AnimeCtrl_Smashu,

        OpeningGame,
        UITitle,
    }

    public static class Index
    {
        public static int ToInt(this EAssetName assetName)
        {
            return (int)assetName;
        }
    }
}