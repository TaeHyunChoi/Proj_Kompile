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

        private bool _isVisualDimmed = false;

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
            if (null == meshFilter) { outSharedMesh = null; return false; }
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

            if (meshRenderer != null && tileSet != null) UpdateMaterialProperties();
        }

        public void SetVisualDimmed(bool isDimmed)
        {
            if (_isVisualDimmed == isDimmed) return;
            _isVisualDimmed = isDimmed;
            if (meshRenderer != null && tileSet != null) UpdateMaterialProperties();
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
            if (meshRenderer == null || meshRenderer.sharedMaterial == null || meshRenderer.sharedMaterial.mainTexture == null) return Vector2.zero;
            Texture texture = meshRenderer.sharedMaterial.mainTexture;
            int columnIndex = (int)type % 8; int rowIndex = (int)type / 8;
            return new Vector2(columnIndex * (SPRITE_WIDTH / (float)texture.width), 1.0f - (rowIndex + 1) * (SPRITE_HEIGHT / (float)texture.height));
        }

        private Vector2 CalculateUVScale()
        {
            if (meshRenderer == null || meshRenderer.sharedMaterial == null || meshRenderer.sharedMaterial.mainTexture == null) return Vector2.one;
            Texture texture = meshRenderer.sharedMaterial.mainTexture;
            return new Vector2(SPRITE_WIDTH / (float)texture.width, SPRITE_HEIGHT / (float)texture.height);
        }

        // [컴파일 에러 해결] 기본 업데이트 시 null 전달
        public void UpdateMesh() { UpdateMesh(null); }

        // [컴파일 에러 해결] byte mask가 아닌 sbyte[] neighborHeights 배열을 받도록 수정
        public void UpdateMesh(sbyte[] neighborHeights)
        {
            if (meshFilter == null) return;

            Mesh newMesh = MapMeshUtil.GenerateMesh(heightData, neighborHeights);
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

        public void ModifyHeightIndex(int pointIndex, int delta)
        {
            int newVal = heightData[pointIndex] + delta;
            heightData[pointIndex] = (sbyte)Mathf.Clamp(newVal, -1, 8);

            UpdateHeightMask();
            UpdateMesh();

            EditorUtility.SetDirty(this);
        }

        public sbyte GetHeightData(int index) => heightData[index];

        public Vector3 GetPointLocalPos(int index)
        {
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

        /// <summary>
        /// [고도화] 주변 타일의 정점 높이를 가져와 맞닿는 면적만큼만 옆면을 생성합니다.
        /// </summary>
        public void OptimizeSides(Dictionary<Vector2Int, EditMapTileComponent> tileMap)
        {
            Vector3 pos = transform.position;
            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z));

            sbyte[] nh = new sbyte[16];

            FillNeighborHeights(nh, 0, tileMap, gridPos + Vector2Int.down, 10, 11, 12); // Back 
            FillNeighborHeights(nh, 2, tileMap, gridPos + Vector2Int.right, 0, 5, 10);   // Right 
            FillNeighborHeights(nh, 4, tileMap, gridPos + Vector2Int.up, 2, 1, 0);    // Forward 
            FillNeighborHeights(nh, 6, tileMap, gridPos + Vector2Int.left, 12, 7, 2);   // Left 

            UpdateMesh(nh);
        }

        private void FillNeighborHeights(sbyte[] nh, int startSeg, Dictionary<Vector2Int, EditMapTileComponent> map, Vector2Int nPos, int nV1, int nV2, int nV3)
        {
            if (map.TryGetValue(nPos, out var neighbor))
            {
                nh[startSeg * 2] = neighbor.GetHeightData(nV1);
                nh[startSeg * 2 + 1] = neighbor.GetHeightData(nV2);
                nh[(startSeg + 1) * 2] = neighbor.GetHeightData(nV2);
                nh[(startSeg + 1) * 2 + 1] = neighbor.GetHeightData(nV3);
            }
            else
            {
                nh[startSeg * 2] = nh[startSeg * 2 + 1] = nh[(startSeg + 1) * 2] = nh[(startSeg + 1) * 2 + 1] = -1;
            }
        }
    }
}
#endif