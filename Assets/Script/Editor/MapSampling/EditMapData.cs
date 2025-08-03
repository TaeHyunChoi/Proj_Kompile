#if UNITY_EDITOR
namespace Script.Data
{
    using System;
    using UnityEngine;
    using System.Threading.Tasks;
    using Script.Util;
    using Script.Index;
    using static Script.Index.Index;

    [Serializable]   // 에셋으로 저장하기 위함
    [ExecuteAlways]  // 에디터에서 텍스쳐 곧장 적용하기 위함
    public class EditMapData : MonoBehaviour
    {
        private const int SPRITE_WIDTH  = 256;
        private const int SPRITE_HEIGHT = 256;

        [Header("Render")]
        [SerializeField] private bool isOnlyRender;
        [SerializeField] private int layer;
        [SerializeField] private TextureIndex textureType;

        private MeshFilter  meshFilter;
        private MeshRenderer meshRenderer;
        private ulong naviMask;
        private uint  infoMask;
        private int   gridKey;

        public MeshFilter MeshFilter => meshFilter;
        public MeshRenderer MeshRenderer => meshRenderer;
        public int Layer => layer;
        public int GridKey => gridKey;
        public int TextureIndex => (int)textureType;

        // Bake(Set) NavMesh Info
        public void InitNaviMask(int[] heights, bool isSmall)
        {
            int i = 0;
            foreach (int height in heights)
            {
                ulong h;
                if (-1 == height)
                {
                    h = 0b1111;
                }
                else
                {
                    h = (ulong)height;
                }

                naviMask |= h << i;
                i += 4;
            }

            if (true == isSmall)
            {
                naviMask |= 1ul << i;
            }
        }
        public async Task BakeMesh(ConcurrentDictionary<int, MapGridData> map)
        {
            if (true == isOnlyRender)
            {
                return;
            }

            // get: (rotated) pivot
            bool isSmall = (naviMask >> (4 * 13)) != 0;
            int rotInt = (transform.rotation.y).ToInt();
            rotInt = (rotInt + 360) % 360;
            if (rotInt % 90 != 0)
            {
                Debug.LogError($"Tile has Wrong Rotation; ({rotInt})");
                return;
            }
            await Task.Yield();

            // get: pivot key
            GetPivotRotated(rotInt, isSmall, out Vector3 gridPivot, out Vector3 tilePivot);
            gridKey = GetGridKeyMask(gridPivot);
            int tileKey = GetTileKeyMask(gridPivot, tilePivot, isSmall);


            // set: map
            map.TryAdd(gridKey, new MapGridData(gridKey));


            // set: map[grid].NavMesh
            naviMask = GetNaviMaskRotated(rotInt, isSmall); // ?? of 64 bits used
            infoMask = GetInfoMask();
            map[gridKey].TryAddNavMeshData(tileKey, new MapNavData(naviMask, infoMask));


            // set: map[grid].Render
            // ...

        }
        private void GetPivotRotated(int rot, bool isSmall, out Vector3 gridPivot, out Vector3 tilePivot)
        {
            // tile pivot
            Vector3 rotated;
            switch (rot)
            {
                case 90: rotated = new Vector3(0f, 0f, -1f); break;
                case 180: rotated = new Vector3(-1f, 0f, -1f); break;
                case 270: rotated = new Vector3(-1f, 0f, 0f); break;
                default: rotated = Vector3.zero; break;
            }
            rotated *= isSmall ? 0.5f : 1f;
            tilePivot = transform.position + rotated;

            // grid pivot
            var gx = Mathf.FloorToInt(tilePivot.x / GRID_X_LENGTH);
            var gy = Mathf.FloorToInt(tilePivot.y / GRID_Y_LENGTH);
            var gz = Mathf.FloorToInt(tilePivot.z / GRID_Z_LENGTH);
            gridPivot = new Vector3(gx, gy, gz);
        }
        private int GetGridKeyMask(Vector3 gridPivot)
        {
            // 비트 쉬프트: 2진법 기준으로 오른쪽(작은 수)부터 z,y,x 이므로 상대적으로 z를 먼저 지정
            const int SHIFT_GRID_Z = 0;
            const int SHIFT_GRID_Z_SIGN = FIELD_HALF_LENGTH;

            const int SHIFT_GRID_Y = SHIFT_GRID_Z_SIGN + 1;
            const int SHIFT_GRID_Y_SIGN = SHIFT_GRID_Y + FIELD_HALF_LENGTH;

            const int SHIFT_GRID_X = SHIFT_GRID_Y_SIGN + 1;
            const int SHIFT_GRID_X_SIGN = SHIFT_GRID_X + FIELD_HALF_LENGTH;


            Vector3Int gridInt = gridPivot.ToInt();
            int gridFlag = 0;

            int x = gridInt.x;
            int y = gridInt.y;
            int z = gridInt.z;

            if (x < 0)
            {
                gridFlag |= 1 << SHIFT_GRID_X_SIGN;
                x *= -1;
            }
            gridFlag |= x << SHIFT_GRID_X;

            if (y < 0)
            {
                gridFlag |= 1 << SHIFT_GRID_Y_SIGN;
                y *= -1;
            }
            gridFlag |= y << SHIFT_GRID_Y;

            if (z < 0)
            {
                gridFlag |= 1 << SHIFT_GRID_Z_SIGN;
                z *= -1;
            }
            gridFlag |= z << SHIFT_GRID_Z;

            return (ushort)gridFlag;
        }
        private int GetTileKeyMask(Vector3 gridPivot, Vector3 tilePivot, bool isSmall)
        {
            gridPivot = new Vector3(gridPivot.x * 32, gridPivot.y * 4, gridPivot.z * 32);

            Vector3 diff = tilePivot - gridPivot;
            if (true == isSmall)
            {
                diff *= 2f;
            }
            Vector3Int diffInt = diff.ToInt();

            // scale ,x[sign,small_buffer,6], y[sign,small_buffer,4], z[sign,small_buffer,6]
            const byte shiftTileLayer = 23;
            const byte shiftIsHalfScale = 22;
            const byte shiftTileX = 14;
            const byte shiftTileY = 8;
            const byte shiftTileZ = 0;

            int mask = 0;
            mask |= layer << shiftTileLayer;
            mask |= isSmall ? 1 << shiftIsHalfScale : 0;
            mask |= (diffInt.x) << shiftTileX;
            mask |= (diffInt.y) << shiftTileY;
            mask |= (diffInt.z) << shiftTileZ;

            return mask;
        }

        // [NavMesh] Height
        private ulong GetNaviMaskRotated(int rot, bool isSmall)
        {
            int[,] matrix = GetHeightMatrixRotated(rot);

            ulong newMask = 0;
            ulong mask = 0;

            int shift;
            for (shift = 0; shift < 13; ++shift)
            {
                switch (shift)
                {
                    case  0: mask = (ulong)matrix[0, 4]; break;
                    case  1: mask = (ulong)matrix[2, 4]; break;
                    case  2: mask = (ulong)matrix[4, 4]; break;
                    case  3: mask = (ulong)matrix[1, 3]; break;
                    case  4: mask = (ulong)matrix[3, 3]; break;
                    case  5: mask = (ulong)matrix[0, 2]; break;
                    case  6: mask = (ulong)matrix[2, 2]; break;
                    case  7: mask = (ulong)matrix[4, 2]; break;
                    case  8: mask = (ulong)matrix[1, 1]; break;
                    case  9: mask = (ulong)matrix[3, 1]; break;
                    case 10: mask = (ulong)matrix[0, 0]; break;
                    case 11: mask = (ulong)matrix[2, 0]; break;
                    case 12: mask = (ulong)matrix[4, 0]; break;
                    default: break;
                }

                newMask |= mask << shift * 4;
            }

            if (true == isSmall)
            {
                newMask |= 1ul << (4 * shift);
            }

            return newMask;
        }
        private int[,] GetHeightMatrixRotated(int rot)
        {
            var matrix = new int[5, 5];
            var flag = naviMask;
            for (var i = 0; i < 13; i++)
            {
                var h = (int)(flag & 0b1111);

                switch (i)
                {
                    case 0: matrix[0, 4] = h; break;
                    case 1: matrix[2, 4] = h; break;
                    case 2: matrix[4, 4] = h; break;
                    case 3: matrix[1, 3] = h; break;
                    case 4: matrix[3, 3] = h; break;
                    case 5: matrix[0, 2] = h; break;
                    case 6: matrix[2, 2] = h; break;
                    case 7: matrix[4, 2] = h; break;
                    case 8: matrix[1, 1] = h; break;
                    case 9: matrix[3, 1] = h; break;
                    case 10: matrix[0, 0] = h; break;
                    case 11: matrix[2, 0] = h; break;
                    case 12: matrix[4, 0] = h; break;
                }

                flag >>= 4;
            }

            return RotateMatrix(matrix, rot);
        }
        private int[,] RotateMatrix(int[,] matrix, int rot)
        {
            if (0 == rot)
            {
                return matrix;
            }

            var n = matrix.GetLength(0); // 행렬 크기
            var rotated = new int[n, n];

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    switch (rot)
                    {
                        case 270:
                            rotated[j, n - 1 - i] = matrix[i, j];
                            break;
                        case 180:
                            rotated[n - 1 - i, n - 1 - j] = matrix[i, j];
                            break;
                        case 90:
                            rotated[n - 1 - j, i] = matrix[i, j];
                            break;
                        default:
                            break;
                    }
                }
            }

            return rotated;
        }


        // [NavMesh] Info
        private uint GetInfoMask()
        {
            // not yet developed;
            return 0;
        }

        // [Render] Texture
        private void OnValidate()
        {
            ApplyTexture();
        }
        private void ApplyTexture()
        {
            meshFilter = transform.GetComponent<MeshFilter>();
            meshRenderer = transform.GetComponent<MeshRenderer>();

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

            // ✅ MaterialPropertyBlock을 사용해 개별 속성 적용
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(propertyBlock);

            //propertyBlock.SetColor("_Color", GetColorByEnum(textureType)); // 개별 색상 적용
            propertyBlock.SetVector("_UVOffset", uvOffset); // UV Offset 적용
            propertyBlock.SetVector("_UVScale", uvScale);   // UV Scale 적용

            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        //[Render] 
        // ...
    }
}
#endif
