using Script.Field.Data;

namespace Script.Field.Manager
{
    using Script.Main;
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

        public async Awaitable InitializeAsync(MainManager main)
        {
            var init_unit = _unitMgr.SpawnUnitAsync("unit_prefab", Vector3.zero, Data.UnitType.Player, FieldBrainType.PlayerControl);
            var init_map  = _mapMgr.InitializeAsync(main.Cam.transform);

            await init_unit;
            await init_map;
        }

        // --- map ---
        public async Awaitable Map_InitializeAsync(Transform camTransform)
        {
            await _mapMgr.InitializeAsync(camTransform);
        }
        public async Awaitable UpdateLayerAsync(int layer)
        {
            await _mapMgr.UpdateLayerVisibilityAsync(layer, false, 0.10f);
        }
    }

}