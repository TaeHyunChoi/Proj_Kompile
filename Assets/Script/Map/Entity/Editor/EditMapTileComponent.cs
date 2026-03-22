#if UNITY_EDITOR
namespace Script.Map.Data
{
    using Script.Index;
    using Script.Map.Utility;
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using static Script.Map.Data.MapConsts;

    /// <summary>
    /// [Framework] Component: 맵 타일의 외형(Mesh)과 데이터(HeightMask)를 관리합니다.
    /// </summary>
    [Serializable]
    [ExecuteInEditMode]
    public class EditMapTileComponent : MonoBehaviour
    {
        private const int SPRITE_WIDTH = 256;
        private const int SPRITE_HEIGHT = 256;

        [Header("Render")]
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private bool isOnlyRender;
        [SerializeField] private ushort renderLayer;

        [SerializeField] private TileSetDefinition tileSet;

        [Header("Data")]
        [SerializeField] private ulong heightMask;
        [SerializeField] private MapTileHeightsData heightData = new MapTileHeightsData();

        // --- 에디터 시각화용 변수 ---
        private bool _isVisualDimmed = false;

        // --- Properties ---
        public int GridKey => MapCoordUtil.ComputeGridKey(transform.position);
        public ushort RenderLayer => renderLayer;
        public int TextureIndex => tileSet != null ? (int)tileSet.topTexture : 0;
        public ulong HeightMask => heightMask;
        public TileSetDefinition TileSet => tileSet;

        private void Awake()
        {
            if (!meshFilter) meshFilter = GetComponent<MeshFilter>();
            if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();
            heightData.EnsureInitialized();
        }

        public bool TryGetSharedMesh(out Mesh outSharedMesh)
        {
            if (null == meshFilter)
            {
                outSharedMesh = null;
                return false;
            }
            outSharedMesh = meshFilter.sharedMesh;
            return null != outSharedMesh;
        }

        public void InitializePrefab(int[] heights, bool isSmall)
        {
            heightMask = 0;
            for (int i = 0; i < heights.Length; ++i)
            {
                int height = heights[i];
                ulong heightFlag = (-1 == height) ? HEIGHT_MASK : (ulong)height;
                heightMask |= (heightFlag & HEIGHT_MASK) << (i * HEIGHT_BITS);

                if (i < 13) heightData[i] = (sbyte)height;
            }
            EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            if (this == null) return;
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();

            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                UpdateHeightMask();
                UpdateMesh();
                SceneView.RepaintAll();
            };

            if (meshRenderer == null || meshRenderer.sharedMaterial == null || meshRenderer.sharedMaterial.mainTexture == null) return;

            if (tileSet != null)
            {
                UpdateMaterialProperties();
            }
        }

        public void SetVisualDimmed(bool isDimmed)
        {
            if (_isVisualDimmed == isDimmed) return;
            _isVisualDimmed = isDimmed;

            if (meshRenderer != null && tileSet != null)
            {
                UpdateMaterialProperties();
            }
        }

        public void SetRenderLayer(ushort layer)
        {
            renderLayer = layer;
            EditorUtility.SetDirty(this);
        }

        private void UpdateMaterialProperties()
        {
            Vector2 topUVOffset = CalculateUVOffset(tileSet.topTexture);
            Vector2 topUVScale = CalculateUVScale();

            Vector2 sideUVOffset = CalculateUVOffset(tileSet.sideTexture);
            Vector2 sideUVScale = topUVScale;

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetVector("_TopUVOffset", topUVOffset);
            propertyBlock.SetVector("_TopUVScale", topUVScale);
            propertyBlock.SetVector("_SideUVOffset", sideUVOffset);
            propertyBlock.SetVector("_SideUVScale", sideUVScale);

            Color tintColor = _isVisualDimmed ? new Color(0.2f, 0.2f, 0.2f, 1.0f) : Color.white;
            propertyBlock.SetColor("_Color", tintColor);

            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private Vector2 CalculateUVOffset(MapTextureType type)
        {
            if (meshRenderer == null || meshRenderer.sharedMaterial == null || meshRenderer.sharedMaterial.mainTexture == null)
                return Vector2.zero;

            Texture texture = meshRenderer.sharedMaterial.mainTexture;
            int textureWidth = texture.width;
            int textureHeight = texture.height;

            int columnIndex = (int)type % 8;
            int rowIndex = (int)type / 8;

            float uMin = columnIndex * (SPRITE_WIDTH / (float)textureWidth);
            float vMin = 1.0f - (rowIndex + 1) * (SPRITE_HEIGHT / (float)textureHeight);

            return new Vector2(uMin, vMin);
        }

        private Vector2 CalculateUVScale()
        {
            if (meshRenderer == null || meshRenderer.sharedMaterial == null || meshRenderer.sharedMaterial.mainTexture == null)
                return Vector2.one;

            Texture texture = meshRenderer.sharedMaterial.mainTexture;
            float uScale = SPRITE_WIDTH / (float)texture.width;
            float vScale = SPRITE_HEIGHT / (float)texture.height;

            return new Vector2(uScale, vScale);
        }

        public void UpdateMesh()
        {
            if (meshFilter == null) return;

            // [핵심] 인자가 1개인 새로운 GenerateMesh 로직 사용
            Mesh newMesh = MapMeshUtil.GenerateMesh(heightData);
            newMesh.name = "Generated3DBlockMesh";

            if (meshFilter.sharedMesh != null && meshFilter.sharedMesh.name == "Generated3DBlockMesh")
            {
                DestroyImmediate(meshFilter.sharedMesh, true);
            }
            meshFilter.sharedMesh = newMesh;

            if (TryGetComponent<MeshCollider>(out var mc)) mc.sharedMesh = newMesh;
        }

        public void UpdateHeightMask()
        {
            heightMask = 0;
            for (int i = 0; i < 13; i++)
            {
                int hValue = heightData[i];
                ulong heightFlag = (-1 == hValue) ? HEIGHT_MASK : (ulong)hValue;
                heightMask |= (heightFlag & HEIGHT_MASK) << (i * HEIGHT_BITS);
            }
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// 정점의 높이를 조절합니다. y가 0일 때 delta가 -1이면 소멸(-1) 상태가 됩니다.
        /// </summary>
        public void ModifyHeightIndex(int pointIndex, int delta)
        {
            int newVal = heightData[pointIndex] + delta;
            // [핵심 수정] Clamp의 최소값을 0에서 -1로 변경하여 소멸(삭제) 상태를 허용합니다.
            heightData[pointIndex] = (sbyte)Mathf.Clamp(newVal, -1, 8);

            UpdateHeightMask();
            UpdateMesh();

            EditorUtility.SetDirty(this);
        }

        // [신규] 에디터에서 현재 정점의 상태를 가져오기 위한 유틸리티 함수
        public sbyte GetHeightData(int index)
        {
            return heightData[index];
        }

        public Vector3 GetPointLocalPos(int index)
        {
            // 소멸(-1)된 정점은 핸들이 지하로 꺼지지 않도록 y를 0으로 고정하여 표시합니다.
            float y = (heightData[index] == -1) ? 0f : (heightData[index] * MapMeshUtil.HeightStep);
            return new Vector3(0, y, 0);
        }

        public void ApplyTileSet(TileSetDefinition newTileSet)
        {
            if (tileSet == newTileSet || newTileSet == null) return;

            Undo.RecordObject(this, "Paint TileSet");
            tileSet = newTileSet;
            EditorUtility.SetDirty(this);

            if (meshRenderer != null && meshRenderer.sharedMaterial != null)
            {
                UpdateMaterialProperties();
            }
        }
    }
}
#endif