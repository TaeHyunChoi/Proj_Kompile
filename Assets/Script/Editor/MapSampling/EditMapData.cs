#if UNITY_EDITOR
namespace Script.Data
{
    using Script.Index;
    using Script.Util;
    using static Script.Index.MapTileIndex;
    using System;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEngine;

    [Serializable]       // 에셋으로 저장하기 위함
    [ExecuteInEditMode]  // 에디터에서 텍스쳐 곧장 적용하기 위함
    public class EditMapData : MonoBehaviour
    {
        private const int SPRITE_WIDTH  = 256;
        private const int SPRITE_HEIGHT = 256;

        private const int TOTAL_BITS = 13;
        private const int BITS_PER_CELL = 4;
        private const int MATRIX_SIZE = 5;

        private static readonly Vector2Int[] INDEX_MAP = new Vector2Int[]
            {
                new Vector2Int(0, 4), new Vector2Int(2, 4), new Vector2Int(4, 4),
                new Vector2Int(1, 3), new Vector2Int(3, 3),
                new Vector2Int(0, 2), new Vector2Int(2, 2), new Vector2Int(4, 2),
                new Vector2Int(1, 1), new Vector2Int(3, 1),
                new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(4, 0)
            };

        [Header("Render")]
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private bool isOnlyRender;
        [SerializeField] private int renderLayer;
        [SerializeField] private TextureIndex textureType;

        [Header("Data")]
        [SerializeField] private int naviLayer;
        [SerializeField] private ulong heightMask;

        //private int gridKey;
        [SerializeField] private uint infoMask;

        private bool isSmall;

        public int GridKey { get; private set; }
        public MeshFilter MeshFilter => meshFilter;
        public int RenderLayer => renderLayer;
        public int NaviLayer => naviLayer;
        public int TextureIndex => (int)textureType;

        private void Awake()
        {
            // ExecuteInEditMode 라서 Edit Mode 에서도 호출된다.
            meshFilter = transform.GetComponent<MeshFilter>();
            meshRenderer = transform.GetComponent<MeshRenderer>();
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
                height      = heights[i];
                heightFlag  = (-1 == height) ? HEIGHT_MASK : (ulong)height;
                heightMask |= heightFlag << i * HEIGHT_BITS;
            }

            this.isSmall = isSmall;

            EditorUtility.SetDirty(this);
        }



        public async Task Bake(int sceneIndex, ConcurrentDictionary<int, MapGridData> map)
        {
            if (true == isOnlyRender)
            {
                return;
            }

            Vector3 position = transform.position;
            float rotY = transform.eulerAngles.y;
            await Task.Yield();

            // 모든 타일이 1*1 또는 0.5f*0.5f 격자에 맞춰서 배치되어 있음
            // 즉, 현재 타일에 회전값을 적용하면 tile_pivot 값이 나온다.
            // tile_pivot을 기준으로 grid_pivot값을 구한다.

            Vector3 gridPivot = EditMapUtil.GetGridPivot(position, rotY);
            int gridKey = EditMapUtil.GetGridKeyMask(sceneIndex, gridPivot);
            GridKey = gridKey;

            Vector3 tilePivot = EditMapUtil.GetTilePivot(position, rotY, isSmall);
            int tileKey = EditMapUtil.GetTileKeyMask(gridPivot, tilePivot, isSmall);

            long naviMask = GetRotatedHeightMask(rotY);
            //infoMask = GetInfoMask();

            map.TryAdd(gridKey, new MapGridData(gridKey));
            map[gridKey].TryAdd(tileKey, new MapTileData(naviMask, infoMask));
        }


        private long GetRotatedHeightMask(float rotY)
        {
            int rotInt = Mathf.RoundToInt(rotY);
            rotInt = (rotInt + 360) % 360;
            if (rotInt % 90 != 0)
            {
                Debug.LogError($"Tile has Wrong Rotation; ({rotInt})");
                return 0;
            }

            ulong[,] heightMatrix  = BitmaskToMatrix(heightMask);
            ulong[,] rotatedMatrix = RotateMatrix(heightMatrix, rotInt);
            ulong    rotatedHeightMask   = MatrixToBitmask(rotatedMatrix);

            ulong layerMask = (ulong)naviLayer << (TOTAL_BITS * BITS_PER_CELL);

            return (long)(layerMask | rotatedHeightMask);
        }
        private ulong[,] BitmaskToMatrix(ulong mask)
        {
            ulong[,] matrix = new ulong[MATRIX_SIZE, MATRIX_SIZE];
            ulong cellValue;
            int row, col;

            for (int i = 0; i < TOTAL_BITS; ++i)
            {
                cellValue = mask & HEIGHT_MASK;
                row = INDEX_MAP[i].x;
                col = INDEX_MAP[i].y;

                matrix[row, col] = cellValue;
                mask >>= BITS_PER_CELL;
            }

            return matrix;
        }
        private ulong[,] RotateMatrix(ulong[,] matrix, int rot)
        {
            if (0 == rot)
            {
                return matrix;
            }

            ulong[,] rotated = new ulong[MATRIX_SIZE, MATRIX_SIZE];
            for (int i = 0; i < MATRIX_SIZE; i++)
            {
                for (int j = 0; j < MATRIX_SIZE; j++)
                {
                    switch (rot)
                    {
                        case 90:
                            rotated[j, MATRIX_SIZE - 1 - i] = matrix[i, j];
                            break;
                        case 180:
                            rotated[MATRIX_SIZE - 1 - i, MATRIX_SIZE - 1 - j] = matrix[i, j];
                            break;
                        case 270:
                            rotated[MATRIX_SIZE - 1 - j, i] = matrix[i, j];
                            break;
                    }
                }
            }
            return rotated;
        }
        private ulong MatrixToBitmask(ulong[,] matrix)
        {
            ulong newMask = 0ul;
            ulong mask;

            int row, col;
            for (int i = 0; i < TOTAL_BITS; ++i)
            {
                row = INDEX_MAP[i].x;
                col = INDEX_MAP[i].y;
                mask = matrix[row, col];

                newMask |= mask << i * BITS_PER_CELL;
            }

            return newMask;
        }


        private void OnValidate()
        {
            // ApplyTexture();

            // 공유된 Material 유지
            Texture texture = meshRenderer.sharedMaterial.mainTexture;
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
