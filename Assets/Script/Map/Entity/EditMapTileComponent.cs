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

        private bool _isVisualDimmed = false;

        public MeshFilter MeshFilter => meshFilter;
        public MeshRenderer MeshRenderer => meshRenderer;
        public ushort RenderLayer => renderLayer;
        public int TopTextureIndex => topTextureIndex;
        public int SideTextureIndex => sideTextureIndex;
        public Texture2D TopAtlasTexture => topAtlasTexture;
        public Texture2D SideAtlasTexture => sideAtlasTexture;
        public ulong HeightMask => heightMask;

        [SerializeField] private MapTileHeightsData heightData = new MapTileHeightsData();
        public MapTileHeightsData HeightData 
        { 
            get => heightData; 
            set => heightData = value; 
        }

#if UNITY_EDITOR
        // 에디터 로직을 호출하기 위한 정적 대리자 (에디터 어셈블리에서 구독함)
        public static System.Action<EditMapTileComponent> OnEditorDataChanged;
#endif
        
        private void Awake()
        {
            if (!meshFilter) meshFilter = GetComponent<MeshFilter>();
            if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();
            heightData.EnsureInitialized();
        }
        private void OnEnable()
        {
            UpdateMaterialProperties();
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
            if (meshRenderer.HasPropertyBlock()) meshRenderer.GetPropertyBlock(pb);

            // 1. 오프셋 계산 (기존 유지)
            Vector2 topOffset = new Vector2((topTextureIndex % 8) * UV_STEP, 1f - ((topTextureIndex / 8 + 1) * UV_STEP));
            Vector2 sideOffset = new Vector2((sideTextureIndex % 8) * UV_STEP, 1f - ((sideTextureIndex / 8 + 1) * UV_STEP));
            
            // 2. 스케일 계산 (복원!)
            Vector2 uvScale = new Vector2(UV_STEP, UV_STEP);

            // 3. 쉐이더 프로퍼티 주입
            pb.SetVector("_TopUVOffset", topOffset);
            pb.SetVector("_TopUVScale", uvScale);   // <-- 누락되었던 핵심 스케일 값 복구
            pb.SetVector("_SideUVOffset", sideOffset);
            pb.SetVector("_SideUVScale", uvScale);  // <-- 누락되었던 핵심 스케일 값 복구
            
            pb.SetFloat("_IsBaked", 0f);            // <-- 에디터 프리뷰용 플래그 복구

            if (topAtlasTexture != null)
            {
                pb.SetTexture("_TopAtlas", topAtlasTexture);
                pb.SetTexture("_MainTex", topAtlasTexture); // URP/Standard 호환용 Fallback 복구
                pb.SetTexture("_BaseMap", topAtlasTexture);
            }
            if (sideAtlasTexture != null)
            {
                pb.SetTexture("_SideAtlas", sideAtlasTexture);
            }
            
            Color tint = _isVisualDimmed ? new Color(0.2f, 0.2f, 0.2f, 1f) : Color.white;
            pb.SetColor("_Color", tint);

            meshRenderer.SetPropertyBlock(pb);
        }
        
#if UNITY_EDITOR
        // 인스펙터 조작 및 Undo/Redo 시 호출됨
        private void OnValidate()
        {
            if (Application.isPlaying) return;

            // 유니티 씬 로딩 및 Undo 복구 타이밍 이슈를 방지하기 위해 한 프레임 뒤에 실행
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                
                // 1. 머티리얼 속성 복구 (텍스처 소실 방지)
                UpdateMaterialProperties();

                // 2. 에디터 측에 데이터 변경 신호 전달 (메쉬 갱신용)
                OnEditorDataChanged?.Invoke(this);
            };
        }
#endif
        
    }
}