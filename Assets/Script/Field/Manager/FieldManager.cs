namespace Script.Field.Manager
{
    using Script;
    using Script.Map.Manager;
    using UnityEngine;

    public class FieldManager
    {
        private readonly MapManager _mapMgr;
        private readonly FieldUnitManager _unitMgr;

        public FieldManager(MainManager main)
        {
            _mapMgr  = new MapManager(main.MapRoot);
            _unitMgr = new FieldUnitManager(main.UnitRoot);
        }

        // --- map ---
        public async Awaitable Map_InitializeAsync(Transform camTransform)
        {
            await _mapMgr.InitializeAsync(camTransform);
        }
        public async Awaitable UpdateLayer(int layer)
        {
            await _mapMgr.UpdateLayerVisibilityAsync(layer, false, 0.10f);
        }
    }

}