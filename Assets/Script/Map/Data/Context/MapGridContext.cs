namespace Script.Map.Data
{
    using UnityEngine;
    using Script.Map.Data;
    using Script.Map.Entity;
    
    public class MapGridContext
    {
        private const float GRID_SIZE = 64f;
        
        public int GridKey { get; private set; }
        public Vector3Int GridIndex { get; private set; }
        public MapGridData Data { get; private set; }
        public MapGridEntity VisualObject { get; private set; }
        public Bounds WorldBounds { get; private set; }

        public MapGridContext(int gridKey, Vector3Int gridPivot)
        {
            GridKey = gridKey;
            GridIndex = gridPivot;
            CalculateBounds();
        }

        private void CalculateBounds()
        {
            float gridSize = GRID_SIZE;
            float minX = GridIndex.x * gridSize;
            float minY = GridIndex.y * gridSize;
            float minZ = GridIndex.z * gridSize;

            float cx = minX + (gridSize * 0.5f);
            float cy = minY + (gridSize * 0.5f);
            float cz = minZ + (gridSize * 0.5f);
            
            Vector3 center = new Vector3(cx, cy, cz);
            Vector3 size = gridSize * Vector3.one;
            
            WorldBounds = new Bounds(center, size);
        }

        public void SetData(MapGridData data)
        {
            Data = data;
        }
        public void SetVisualObject(MapGridEntity visualObject)
        {
            VisualObject = visualObject;
        }

        /// <summary> 카메라 가시성과 타겟 레이어 마스크를 동시에 반영하여 활성 상태를 갱신 </summary>
        public void UpdateVisibility(bool isFrustumVisible, int targetLayerMask)
        {
            if (false == VisualObject)
            {
                return;
            }
            
            // 카메라 밖이면 전체 비활성화
            if (false == isFrustumVisible)
            {
                if (true == VisualObject.gameObject.activeSelf)
                {
                    VisualObject.gameObject.SetActive(false);
                }

                return;
            }
            
            // 카메라 안이면 전체 활성화 후 레이어 필터링
            if (false == VisualObject.gameObject.activeSelf)
            {
                VisualObject.gameObject.SetActive(true);
            }
            
            VisualObject.UpdateLayerVisibility(targetLayerMask);
        }

        public void Dispose()
        {
            Data = null;
            VisualObject = null;
        }
    }
}