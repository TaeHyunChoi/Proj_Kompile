#if UNITY_EDITOR
namespace Script.Map.Data
{
    using Script.Map.Utility;
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using static Script.Map.Data.MapConsts;

    [Serializable]
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class EditMapTileComponent : MonoBehaviour
    {
        private const float UV_STEP = 1f / 8f;

        [Header("Render")]
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private bool isOnlyRender;
        [SerializeField] private ushort renderLayer;

        [Header("Atlas Textures (면별 독립 아틀라스)")]
        // [나으리 아이디어 적용] 윗면과 옆면이 서로 다른 아틀라스를 참조할 수 있도록 분리합니다.
        [SerializeField] private Texture2D topAtlasTexture;
        [SerializeField] private Texture2D sideAtlasTexture;

        [SerializeField] private int topTextureIndex = 0;
        [SerializeField] private int sideTextureIndex = 0;

        [Header("Data")]
        [SerializeField] private ulong heightMask;
        [SerializeField] private MapTileHeightsData heightData = new MapTileHeightsData();

        private bool _isVisualDimmed = false;

        public int GridKey => MapCoordUtil.ComputeGridKey(transform.position);
        public ushort RenderLayer => renderLayer;

        public int TopTextureIndex => topTextureIndex;
        public int SideTextureIndex => sideTextureIndex;

        // 스포이드 시 에디터가 읽어갈 수 있도록 프로퍼티 개방
        public Texture2D TopAtlasTexture => topAtlasTexture;
        public Texture2D SideAtlasTexture => sideAtlasTexture;

        public ulong HeightMask => heightMask;

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

            if (meshRenderer != null) UpdateMaterialProperties();
        }

        public void SetVisualDimmed(bool isDimmed)
        {
            if (_isVisualDimmed == isDimmed) return;
            _isVisualDimmed = isDimmed;
            if (meshRenderer != null) UpdateMaterialProperties();
        }

        public void SetRenderLayer(ushort layer)
        {
            renderLayer = layer;
            EditorUtility.SetDirty(this);
        }

        private void UpdateMaterialProperties()
        {
            Vector2 topUVOffset = CalculateUVOffset(topTextureIndex);
            Vector2 topUVScale = CalculateUVScale();
            Vector2 sideUVOffset = CalculateUVOffset(sideTextureIndex);
            Vector2 sideUVScale = topUVScale;

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            if (meshRenderer.HasPropertyBlock()) meshRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetVector("_TopUVOffset", topUVOffset);
            propertyBlock.SetVector("_TopUVScale", topUVScale);
            propertyBlock.SetVector("_SideUVOffset", sideUVOffset);
            propertyBlock.SetVector("_SideUVScale", sideUVScale);

            // [핵심 변경] 커스텀 쉐이더가 윗면과 옆면 아틀라스를 따로 받을 수 있도록 2개의 프로퍼티에 텍스처를 주입합니다.
            // 💡 주의: 나으리의 커스텀 쉐이더 내부에 _TopAtlas, _SideAtlas 프로퍼티가 정의되어 있어야 완벽하게 동작합니다!
            if (topAtlasTexture != null)
            {
                propertyBlock.SetTexture("_TopAtlas", topAtlasTexture);
                propertyBlock.SetTexture("_MainTex", topAtlasTexture); // 구형 쉐이더용 Fallback
                propertyBlock.SetTexture("_BaseMap", topAtlasTexture); // URP용 Fallback
            }
            if (sideAtlasTexture != null)
            {
                propertyBlock.SetTexture("_SideAtlas", sideAtlasTexture);
            }

            Color tintColor = _isVisualDimmed ? new Color(0.2f, 0.2f, 0.2f, 1.0f) : Color.white;
            propertyBlock.SetColor("_Color", tintColor);

            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private Vector2 CalculateUVOffset(int globalTextureIndex)
        {
            int localIndex = globalTextureIndex % 64;
            int columnIndex = localIndex % 8;
            int rowIndex = localIndex / 8;
            return new Vector2(columnIndex * UV_STEP, 1.0f - ((rowIndex + 1) * UV_STEP));
        }

        private Vector2 CalculateUVScale() => new Vector2(UV_STEP, UV_STEP);

        public void UpdateMesh() { UpdateMesh(null); }

        public void UpdateMesh(sbyte[] neighborHeights)
        {
            if (meshFilter == null) return;
            Mesh newMesh = MapMeshUtil.GenerateMesh(heightData, neighborHeights);
            newMesh.name = "Generated3DBlockMesh";

            if (meshFilter.sharedMesh != null && meshFilter.sharedMesh.name == "Generated3DBlockMesh")
                DestroyImmediate(meshFilter.sharedMesh, true);

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

        // [변경] 브러시로부터 윗면 인덱스/아틀라스, 옆면 인덱스/아틀라스를 각각 독립적으로 부여받습니다.
        public void ApplyTextures(int topIndex, Texture2D tAtlas, int sideIndex, Texture2D sAtlas)
        {
            Undo.RecordObject(this, "Paint Texture Indices");

            topTextureIndex = topIndex;
            sideTextureIndex = sideIndex;
            if (tAtlas != null) topAtlasTexture = tAtlas;
            if (sAtlas != null) sideAtlasTexture = sAtlas;

            EditorUtility.SetDirty(this);

            if (meshRenderer && meshRenderer.sharedMaterial)
            {
                UpdateMaterialProperties();
            }
        }

        public void OptimizeSides(Dictionary<Vector2Int, EditMapTileComponent> tileMap)
        {
            Vector3 pos = transform.position;
            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z));
            sbyte[] nh = new sbyte[16];

            FillNeighborHeights(nh, 0, tileMap, gridPos + Vector2Int.down, 10, 11, 12);
            FillNeighborHeights(nh, 2, tileMap, gridPos + Vector2Int.right, 0, 5, 10);
            FillNeighborHeights(nh, 4, tileMap, gridPos + Vector2Int.up, 2, 1, 0);
            FillNeighborHeights(nh, 6, tileMap, gridPos + Vector2Int.left, 12, 7, 2);

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
            else nh[startSeg * 2] = nh[startSeg * 2 + 1] = nh[(startSeg + 1) * 2] = nh[(startSeg + 1) * 2 + 1] = -1;
        }
    }
}
#endif