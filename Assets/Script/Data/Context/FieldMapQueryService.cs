namespace Kompile.Field.Data
{
    using Kompile.Map.Data;
    using Kompile.Map.Manager;
    using Unity.Mathematics;

    /// <summary> 
    /// MapManager의 그리드 데이터를 IMapQueryService 인터페이스로 얇게 노출합니다. <br/>
    /// 💡 [Kompile DOD] 하위 물리 이동(UnitMoveComponent)용이 아닙니다.
    /// 플레이어 마우스 클릭(A*)이나 NPC/몬스터의 배회, 추적 등 상위 인공지능(Brain)의 메인 스레드 맵 탐색 전용으로 주입됩니다.
    /// </summary>
    public class FieldMapQueryService : IMapQueryService
    {
        private readonly MapManager _mapManager;

        public FieldMapQueryService(MapManager mapManager)
        {
            _mapManager = mapManager;
        }

        public bool TryGetTileData(in float3 worldPos, out MapTileData tileData)
        {
            return _mapManager.TryGetTileData(worldPos, out tileData);
        }
    }
}