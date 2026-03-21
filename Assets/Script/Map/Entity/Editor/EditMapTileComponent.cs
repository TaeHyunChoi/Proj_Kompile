#if UNITY_EDITOR
namespace Script.Map.Data
{
    using Script.Index;
    using Script.Map.Utility;
    using System;
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
        [SerializeField] private MapTextureType textureType = Script.Index.MapTextureType.map_w;

        [Header("Data")]
        [SerializeField] private ulong heightMask;
        // 13개 포인트의 높이 데이터를 저장합니다. (0~8 단계)
        [SerializeField] private MapTileHeightsData heightData = new MapTileHeightsData();

        // --- Properties ---
        public int GridKey => MapCoordUtil.ComputeGridKey(transform.position);
        public ushort RenderLayer => renderLayer;
        public int TextureIndex => (int)textureType;
        public ulong HeightMask => heightMask;

        private void Awake()
        {
            // 참조 및 데이터 초기화
            if (!meshFilter) meshFilter = GetComponent<MeshFilter>();
            if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();
            heightData.EnsureInitialized();
        }

        /// <summary>
        /// [복구 완료] 외부 스크립트에서 타일의 공유 메쉬를 안전하게 가져올 때 사용합니다.
        /// </summary>
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

        /// <summary> 
        /// 프리팹 데이터를 초기화하고 비트마스크를 생성합니다.
        /// </summary>
        public void InitializePrefab(int[] heights, bool isSmall)
        {
            heightMask = 0;
            for (int i = 0; i < heights.Length; ++i)
            {
                int height = heights[i];
                ulong heightFlag = (-1 == height) ? HEIGHT_MASK : (ulong)height;
                heightMask |= (heightFlag & HEIGHT_MASK) << (i * HEIGHT_BITS);

                // 편집용 데이터 배열도 함께 동기화
                if (i < 13) heightData[i] = (byte)height;
            }

            EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            if (this == null) return;

            // 1. 컴포넌트 참조 보장
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();

            // 2. 데이터 및 메쉬 갱신 (텍스처 여부와 상관없이 무조건 실행)
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;

                UpdateHeightMask();
                UpdateMesh();

                // 인스펙터 슬라이더 조작 시 씬 뷰 즉시 반영
                SceneView.RepaintAll();
            };

            // 3. UV 및 텍스처 계산 (머티리얼이 있을 때만 진행)
            if (meshRenderer == null ||
                meshRenderer.sharedMaterial == null ||
                meshRenderer.sharedMaterial.mainTexture == null)
            {
                return;
            }

            UpdateUVs();
        }

        private void UpdateUVs()
        {
            Texture texture = meshRenderer.sharedMaterial.mainTexture;
            if (texture.width == 0 || texture.height == 0) return;

            int textureWidth = texture.width;
            int textureHeight = texture.height;
            int columnIndex = (int)textureType % 8;
            int rowIndex = (int)textureType / 8;

            float uMin = columnIndex * (SPRITE_WIDTH / (float)textureWidth);
            float vMin = 1.0f - (rowIndex + 1) * (SPRITE_HEIGHT / (float)textureHeight);

            Vector2 uvOffset = new Vector2(uMin, vMin);
            Vector2 uvScale = new Vector2(SPRITE_WIDTH / (float)textureWidth, SPRITE_HEIGHT / (float)textureHeight);

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetVector("_UVOffset", uvOffset);
            propertyBlock.SetVector("_UVScale", uvScale);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        public void UpdateMesh()
        {
            if (meshFilter == null) return;

            // MapMeshUtil을 통해 13개 포인트 기반 메쉬 생성
            Mesh newMesh = MapMeshUtil.GenerateMesh(heightData);
            newMesh.hideFlags = HideFlags.DontSave;

            // 기존 임시 메쉬 메모리 해제
            if (meshFilter.sharedMesh != null && meshFilter.sharedMesh.name == "GeneratedTileMesh_Dynamic")
            {
                DestroyImmediate(meshFilter.sharedMesh, true);
            }

            meshFilter.sharedMesh = newMesh;

            // 에디터 레이캐스트 정밀도를 위해 콜라이더 동기화
            MeshCollider mc = GetComponent<MeshCollider>();
            if (mc != null) mc.sharedMesh = newMesh;
        }

        public void UpdateHeightMask()
        {
            heightMask = 0;
            for (int i = 0; i < 13; i++)
            {
                ulong hValue = (ulong)heightData[i];
                // 비트 압축 (MapConsts 설정 참조)
                heightMask |= (hValue & HEIGHT_MASK) << (i * HEIGHT_BITS);
            }
            EditorUtility.SetDirty(this);
        }

        public void ModifyHeightIndex(int pointIndex, int delta)
        {
            int newVal = heightData[pointIndex] + delta;
            heightData[pointIndex] = (byte)Mathf.Clamp(newVal, 0, 8);

            // 툴 조작 시 즉시 데이터 및 외형 동기화
            UpdateHeightMask();
            UpdateMesh();

            EditorUtility.SetDirty(this);
        }

        public Vector3 GetPointLocalPos(int index)
        {
            // MapMeshUtil의 규칙(0.125f step)에 따른 로컬 높이 반환
            float y = heightData[index] * MapMeshUtil.HeightStep;
            return new Vector3(0, y, 0);
        }
    }
}
#endif