namespace Kompile.Field.Data
{
    using Kompile.Map.Data;
    using Kompile.Map.Manager;
    using Unity.Mathematics;

    /// <summary>
    /// [Adapter] MapManager의 그리드 데이터를 IMapQueryService 인터페이스로 노출합니다.
    /// FieldUnitManager → FieldPlayerEntity → UnitMoveComponent 체인에 주입됩니다.
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
