using Kompile.Field.Data;
using Kompile.Map.Manager;

namespace Kompile.Field.Manager
{
    using UnityEngine;

    public class FieldManager
    {
        private MapManager _mapManager;
        private FieldMapQueryService _mapQueryService;

        private readonly Transform _fieldRoot;
        private readonly Transform _mapRoot;

        private bool _isFieldActive;
        
        public IMapQueryService MapQueryService => _mapQueryService;
        
        // --- Constructor --
        public FieldManager(Transform fieldRoot)
        {
            _fieldRoot = fieldRoot;

            _mapRoot = new GameObject("Map").transform;
            _mapRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _mapRoot.SetParent(fieldRoot);
            _mapManager = new MapManager(_mapRoot);
            
            _mapQueryService = new FieldMapQueryService(_mapManager);
            _isFieldActive = false;
        }

        // --- Life Cycle ---       
        public void StartFieldAsync(Transform cameraTransform)
        {
            _isFieldActive = true;
            _ = _mapManager.PlayStreamingAsync(cameraTransform); // fire and forgot
        }
        public void StopField()
        {
            _isFieldActive = false;
            _mapManager.StopStreaming();
        }
        public void Dispose()
        {
            _mapManager.StopStreaming();
        }
        
        // --- Layer Control ----
        // MapTileData에서 public ushort LayerMask; 으로 레이어 판별할 예정
        public async Awaitable UpdateMapLayerAsync()
        {
            
        }
    }
}