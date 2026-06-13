namespace Kompile.Unit.Data
{
    using Kompile.Map.Data;

    /// <summary>
    /// [Framework] Data 계층
    /// Burst Job 내부에서 참조 오버헤드 없이 타일 정보와 기준 높이를 동시 소비하기 위한 구조체입니다.
    /// </summary>
    public struct BurstTileInfo
    {
        public MapTileData TileData;
        public float TileBaseY;
    }
}