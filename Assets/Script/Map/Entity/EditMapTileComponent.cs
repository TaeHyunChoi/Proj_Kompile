namespace Kompile.Map.Entity
{
    using Kompile.Map.Data;
    using Kompile.Map.Utility;
    using UnityEngine;

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class EditMapTileComponent : MonoBehaviour
    {
        [Header("Render")]
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private ushort renderLayer;

        [Header("Atlas")]
        [SerializeField] private Texture2D topAtlasTexture;
        [SerializeField] private Texture2D sideAtlasTexture;
        [SerializeField] private int topTextureIndex = 0;
        [SerializeField] private int sideTextureIndex = 0;

        [Header("Data")]
        [SerializeField] private ulong heightMask;
        [SerializeField] private MapTileHeightsData heightData = new MapTileHeightsData();

        private bool _isVisualDimmed = false;

        public MeshFilter MeshFilter => meshFilter;
        public MeshRenderer MeshRenderer => meshRenderer;
        public ushort RenderLayer => renderLayer;
        public int TopTextureIndex => topTextureIndex;
        public int SideTextureIndex => sideTextureIndex;
        public Texture2D TopAtlasTexture => topAtlasTexture;
        public Texture2D SideAtlasTexture => sideAtlasTexture;
        public ulong HeightMask => heightMask;

        // [수정] 구조체 임시 값 수정을 피하기 위해 Setter 개방
        public MapTileHeightsData HeightData 
        { 
            get => heightData; 
            set => heightData = value; 
        }

        private void Awake()
        {
            if (!meshFilter) meshFilter = GetComponent<MeshFilter>();
            if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();
            heightData.EnsureInitialized();
        }

        public void SetVisualDimmed(bool dim)
        {
            if (_isVisualDimmed == dim) return;
            _isVisualDimmed = dim;
            UpdateMaterialProperties();
        }

        public void SetRenderLayer(ushort layer) => renderLayer = layer;
        public void SetHeightMask(ulong mask) => heightMask = mask;

        public void UpdateMaterialProperties()
        {
            if (!meshRenderer) return;
            const float UV_STEP = 1f / 8f;
            
            MaterialPropertyBlock pb = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(pb);

            Vector2 topOffset = new Vector2((topTextureIndex % 8) * UV_STEP, 1f - ((topTextureIndex / 8 + 1) * UV_STEP));
            Vector2 sideOffset = new Vector2((sideTextureIndex % 8) * UV_STEP, 1f - ((sideTextureIndex / 8 + 1) * UV_STEP));

            pb.SetVector("_TopUVOffset", topOffset);
            pb.SetVector("_SideUVOffset", sideOffset);
            if (topAtlasTexture) pb.SetTexture("_TopAtlas", topAtlasTexture);
            if (sideAtlasTexture) pb.SetTexture("_SideAtlas", sideAtlasTexture);
            
            pb.SetColor("_Color", _isVisualDimmed ? new Color(0.2f, 0.2f, 0.2f, 1f) : Color.white);
            meshRenderer.SetPropertyBlock(pb);
        }
    }
}