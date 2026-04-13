namespace Kompile.Field.Data
{
    using Kompile.Map.Data;
    using Unity.Mathematics;

    /// <summary>
    /// [Interface] Field 레이어가 Map 데이터에 의존하지 않도록 추상화한 맵 조회 서비스.
    /// UnitMoveComponent가 이 인터페이스에만 의존하므로 테스트 및 교체가 용이합니다.
    /// </summary>
    public interface IMapQueryService
    {
        /// <summary>
        /// 월드 좌표에서 해당 위치의 MapTileData를 조회합니다.
        /// </summary>
        /// <param name="worldPos">조회할 월드 좌표</param>
        /// <param name="tileData">조회된 타일 데이터</param>
        /// <returns>로드된 타일이 존재하면 true</returns>
        bool TryGetTileData(in float3 worldPos, out MapTileData tileData);
    }
}
