namespace Script.Map.Manager
{
    using Script.Map.Data;
    using System.Collections.Generic;
    using UnityEngine;

    public class MapVisualManager
    {
        private Camera _targetCamera;
        private Plane[] _frustumPlanes;

        // 현재 활성화할 레이어 마스크 (기본값: -1)
        public int CurrentLayerMask { get; set; } = ~0;

        public void Initialize(Camera cam)
        {
            _frustumPlanes = new Plane[6];
            _targetCamera = cam;
        }

        public Plane[] GetFrustumPlanes() => _frustumPlanes;

        public void UpdateCulling(Dictionary<int, MapGridContext> activeGrids)
        {
            if (false == _targetCamera)
            {
                return;
            }

            GeometryUtility.CalculateFrustumPlanes(_targetCamera, _frustumPlanes);

            foreach (var grid in activeGrids.Values)
            {
                if (false == grid.VisualObject)
                {
                    continue;
                }

                bool isVisible = GeometryUtility.TestPlanesAABB(_frustumPlanes, grid.WorldBounds);
                grid.UpdateVisibility(isVisible, CurrentLayerMask);
            }
        }
    }
}