namespace Script.Field.Manager
{
    using Script;
    using Script.Map.Manager;
    using UnityEngine;

    public class FieldManager
    {
        private readonly MapManager _mapMgr;

        public FieldManager(Main main)
        {
            _mapMgr = new MapManager(main.MapRoot);
        }

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