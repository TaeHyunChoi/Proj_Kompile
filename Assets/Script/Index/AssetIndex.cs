namespace Script.Index
{
    public enum CanvasType
    { 
        NONE = 0,
        CAMERA,
        OVERLAY
    }

    /// <summary>
    /// enum.ToString() 사용하여 어드레서블 에셋 탐색 => 실제 에셋과 파일명을 동일하게 맞출 것
    /// </summary>
    public enum AssetIndex
    {
        NONE = 0,

        OP_TitleObject,

        UI_TitleMenuObject,

        UnitBase,
        AnimCtrl_Ataho,
        AnimCtrl_Linxhang,
        AnimeCtrl_Smashu,
    }

    public static class Index
    {
        public static int ToInt(this AssetIndex assetName)
        {
            return (int)assetName;
        }
    }
}