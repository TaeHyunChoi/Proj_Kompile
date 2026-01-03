#if UNITY_EDITOR
namespace Script.Data
{
    using Script.Index;
    using System;
    using UnityEditor;
    using UnityEngine;
    using static Script.Index.MapTileIndex;

    [Serializable]
    [ExecuteInEditMode]
    public class EditMapTileObject : MonoBehaviour
    {
        private const int SPRITE_WIDTH = 256;
        private const int SPRITE_HEIGHT = 256;

        [Header("Render")]
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private bool isOnlyRender;
        [SerializeField] private ushort renderLayer;
        [SerializeField] private TextureIndex textureType = Script.Index.TextureIndex.map_w;

        [Header("Data")]
        [SerializeField] private ulong heightMask;

        public int GridKey => EditMapUtil.ComputeGridKey(transform.position);
        public ushort RenderLayer => renderLayer;
        public int TextureIndex => (int)textureType;
        public ulong HeightMask => heightMask;

        private void Awake()
        {
            // 런타임/에디터 초기화 보장
            if (!meshFilter) meshFilter = GetComponent<MeshFilter>();
            if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();
        }

        /// <summary> 프리팹 데이터를 초기화 ( != 실제 맵 타일 오브젝트) <br/>
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
            // 1. meshRenderer가 연결되어 있지 않다면 재연결 시도
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            // 2. 방어 코드: 렌더러가 없거나, 머티리얼이 없거나, 텍스처가 없으면 로직을 수행하지 않음
            if (meshRenderer == null ||
                meshRenderer.sharedMaterial == null ||
                meshRenderer.sharedMaterial.mainTexture == null)
            {
                return;
            }

            // 공유된 Material 유지
            Texture texture = meshRenderer.sharedMaterial.mainTexture;

            // 3. 텍스처 크기가 0이거나 유효하지 않은 경우 방지 (드문 경우이나 안전장치)
            if (texture.width == 0 || texture.height == 0) 
                return;

            int textureWidth = texture.width;
            int textureHeight = texture.height;

            int columnIndex = (int)textureType % 8;
            int rowIndex = (int)textureType / 8;

            float uMin = columnIndex * (SPRITE_WIDTH / (float)textureWidth);
            float vMin = 1.0f - (rowIndex + 1) * (SPRITE_HEIGHT / (float)textureHeight);

            Vector2 uvOffset = new Vector2(uMin, vMin); // UV 시작 좌표
            Vector2 uvScale = new Vector2(SPRITE_WIDTH / (float)textureWidth, SPRITE_HEIGHT / (float)textureHeight); // 크기

            // MaterialPropertyBlock을 사용해 개별 속성 적용
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(propertyBlock);

            //propertyBlock.SetColor("_Color", GetColorByEnum(textureType)); // 개별 색상 적용
            propertyBlock.SetVector("_UVOffset", uvOffset); // UV Offset 적용
            propertyBlock.SetVector("_UVScale", uvScale);   // UV Scale 적용

            meshRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
#endif