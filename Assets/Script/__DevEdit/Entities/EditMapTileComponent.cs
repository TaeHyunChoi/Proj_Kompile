#if UNITY_EDITOR
namespace Kompile.Editor.Entities
{
    using Kompile.Data;
    using Kompile.Editor.Utility;
    using UnityEngine;

    /// <summary> 씬 뷰 렌더러와 에디터 편집 데이터를 연결하는 순수 View 바인딩 컴포넌트 </summary>
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

        public MeshFilter MeshFilter => meshFilter ? meshFilter : (meshFilter = GetComponent<MeshFilter>());
        public MeshRenderer MeshRenderer => meshRenderer ? meshRenderer : (meshRenderer = GetComponent<MeshRenderer>());
        public ushort RenderLayer { get => renderLayer; set => renderLayer = value; }
        public int TopTextureIndex { get => topTextureIndex; set => topTextureIndex = value; }
        public int SideTextureIndex { get => sideTextureIndex; set => sideTextureIndex = value; }
        public Texture2D TopAtlasTexture { get => topAtlasTexture; set => topAtlasTexture = value; }
        public Texture2D SideAtlasTexture { get => sideAtlasTexture; set => sideAtlasTexture = value; }
        public ulong HeightMask { get => heightMask; set => heightMask = value; }
        public MapTileHeightsData HeightData { get => heightData; set => heightData = value; }
        public bool IsVisualDimmed => _isVisualDimmed;

        public static System.Action<EditMapTileComponent> OnEditorDataChanged;

        private void Awake()
        {
            if (!meshFilter) meshFilter = GetComponent<MeshFilter>();
            if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();
            heightData.EnsureInitialized();
        }

        private void OnEnable()
        {
            UpdateVisual();
        }

        public void SetVisualDimmed(bool dim)
        {
            if (_isVisualDimmed == dim) return;
            _isVisualDimmed = dim;
            UpdateVisual();
        }

        public void SetRenderLayer(ushort layer) => renderLayer = layer;
        public void SetHeightMask(ulong mask) => heightMask = mask;

        /// <summary>
        /// 계산 로직은 Utility에 위임하여 머티리얼 속성을 갱신합니다.
        /// </summary>
        public void UpdateVisual() => EditMapTileUtil.UpdateMaterialProperties(this, _isVisualDimmed);

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            // 씬 로딩 및 Undo 복구 타이밍 이슈 방지를 위해 딜레이 호출
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null)
                {
                    return;
                }

                // 1. 머티리얼 속성 복구 (텍스처 소실 방지)
                UpdateVisual();

                // 2. 에디터 측에 메쉬 갱신 신호 전달
                OnEditorDataChanged?.Invoke(this);
            };
        }
    }
}
#endif