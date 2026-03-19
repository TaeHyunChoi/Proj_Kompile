#if UNITY_EDITOR
namespace Script.Map.Data
{
    using Script.Index;
    using Script.Map.Utility;
    using System;
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
        [SerializeField] private MapTextureType textureType = Script.Index.MapTextureType.map_w;

        [Header("Data")]
        [SerializeField] private ulong heightMask;
        // [신규 추가] 13개 포인트의 높이 데이터를 저장합니다.
        [SerializeField] private MapTileHeightsData heightData = new MapTileHeightsData();

        public int GridKey => MapCoordUtil.ComputeGridKey(transform.position);
        public ushort RenderLayer => renderLayer;
        public int TextureIndex => (int)textureType;
        public ulong HeightMask => heightMask;

        private void Awake()
        {
            // 런타임/에디터 초기화 보장
            if (!meshFilter) meshFilter = GetComponent<MeshFilter>();
            if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();

            // 데이터 배열이 비어있으면 초기화합니다.
            heightData.EnsureInitialized();
        }

        /// <summary> 
        /// 프리팹 데이터를 초기화 ( != 실제 맵 타일 오브젝트) <br/>
        /// heights, isSmall 데이터만 저장한다.
        /// </summary>
        public void InitializePrefab(int[] heights, bool isSmall)
        {
            int height;
            ulong heightFlag;

            for (int i = 0; i < heights.Length; ++i)
            {
                height = heights[i];
                heightFlag = (-1 == height) ? HEIGHT_MASK : (ulong)height;
                heightMask |= heightFlag << i * HEIGHT_BITS;
            }

            //this.isSmall = isSmall;
            EditorUtility.SetDirty(this);
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

        private void OnValidate()
        {
            // 1. Renderer 및 Filter 방어 코드
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();

            if (meshRenderer == null ||
                meshRenderer.sharedMaterial == null ||
                meshRenderer.sharedMaterial.mainTexture == null)
            {
                return;
            }

            // 2. 텍스처 및 UV 계산 (나으리의 기존 로직 완벽 유지)
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

            // 3. 메쉬 갱신 예약
            // 유니티 정책상 OnValidate 내부에서 즉시 Mesh를 Destroy/생성하면 경고가 발생하므로,
            // delayCall을 사용하여 한 프레임 뒤에 안전하게 메쉬를 업데이트합니다.
            EditorApplication.delayCall += () =>
            {
                if (this != null) // 컴포넌트가 파괴되지 않았는지 확인
                {
                    UpdateMesh();
                }
            };
        }

        // --- [신규 추가 메서드] 높이 조절 및 실시간 메쉬 갱신 ---

        /// <summary>
        /// 현재 heightData를 기반으로 메쉬를 다시 생성하고 콜라이더를 갱신합니다.
        /// </summary>
        public void UpdateMesh()
        {
            if (meshFilter == null) return;

            Mesh newMesh = MapMeshUtil.GenerateMesh(heightData);

            // 씬 파일 용량 최적화: 에디터에서 생성된 임시 메쉬는 씬에 저장하지 않음
            newMesh.hideFlags = HideFlags.DontSave;

            // 기존 임시 메쉬 메모리 누수 방지
            if (meshFilter.sharedMesh != null && meshFilter.sharedMesh.name == "GeneratedTileMesh_Dynamic")
            {
                DestroyImmediate(meshFilter.sharedMesh, true);
            }

            meshFilter.sharedMesh = newMesh;

            // 에디터에서 레이캐스트(클릭)가 제대로 되려면 MeshCollider도 함께 갱신해야 합니다.
            MeshCollider mc = GetComponent<MeshCollider>();
            if (mc != null) mc.sharedMesh = newMesh;
        }

        /// <summary>
        /// 특정 포인트의 높이 인덱스를 수정합니다. 에디터 윈도우에서 호출됩니다.
        /// </summary>
        /// <param name="pointIndex">0 ~ 12 사이의 13개 포인트 인덱스</param>
        /// <param name="delta">더하거나 뺄 값 (예: +1, -1)</param>
        public void ModifyHeightIndex(int pointIndex, int delta)
        {
            int newVal = heightData[pointIndex] + delta;
            // 높이 범위를 -4에서 +4로 제한합니다.
            heightData[pointIndex] = (sbyte)Mathf.Clamp(newVal, -4, 4);
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// 지정된 포인트 인덱스의 로컬 위치(에디터 상의 시각적 가이드용)를 반환합니다.
        /// </summary>
        public Vector3 GetPointLocalPos(int index)
        {
            // MapMeshUtil 내부 로직과 동일하게 높이를 구합니다.
            float y = heightData[index] * MapMeshUtil.HeightStep;
            // 타일 중심을 기준으로 X, Z는 생략하거나 대략적으로 구성할 수 있습니다.
            // 본래는 MapMeshUtil의 PointCoords를 참조하여 X, Z도 정확히 주면 좋습니다.
            return new Vector3(0, y, 0);
        }
    }
}
#endif