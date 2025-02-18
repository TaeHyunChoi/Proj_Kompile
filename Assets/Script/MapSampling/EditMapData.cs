namespace Script.Data
{
    using System;
    using UnityEngine;
    using System.Threading.Tasks;
    using Script.Util;
    using Script.Index;

    [Serializable]   // 에셋으로 저장하기 위함
    [ExecuteAlways]  // 에디터에서 텍스쳐 곧장 적용하기 위함
    public class EditMapData : MonoBehaviour
    {
        private const int SPRITE_WIDTH = 256;
        private const int SPRITE_HEIGHT = 256;

        [Header("Render")]
        [SerializeField] private int layer;

        [SerializeField] private TextureIndex textureType;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private ulong naviMask;
        private uint infoMask;

        public MeshFilter MeshFilter => meshFilter;
        public MeshRenderer MeshRenderer => meshRenderer;
        public int Layer => layer;

        private int gridKey;
        public int GridKey => gridKey;

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
        public async Task BakeMesh(ConcurrentDictionary<int, RawMapGridData> map)
        {
            await Task.Yield();

            // get: (rotated) pivot
            bool isSmall = (naviMask >> (4 * 13)) != 0;
            int rotInt = (transform.rotation.y).ToInt();
            rotInt = (rotInt + 360) % 360;
            if (rotInt % 90 != 0)
            {
                Debug.LogError($"Tile has Wrong Rotation; ({rotInt})");
                return;
            }


            // get: pivot key
            GetPivotRotated(rotInt, isSmall, out Vector3 gridPivot, out Vector3 tilePivot);
            gridKey = GetGridKeyMask(gridPivot);
            int tileKey = GetTileKeyMask(gridPivot, tilePivot, isSmall);


            // set: map
            map.TryAdd(gridKey, new RawMapGridData());


            // set: map[grid].NavMesh
            naviMask = GetNaviMaskRotated(rotInt, isSmall); // ?? of 64 bits used
            infoMask = GetInfoMask();
            map[gridKey].TryAddNavMeshData(tileKey, new RawMapNavData(naviMask, infoMask));


            // set: map[grid].Render
            // ...

        }

//    private void Start()
//    {

//        Texture texture = meshRenderer.sharedMaterial.mainTexture;
//        int textureWidth = texture.width;
//        int textureHeight = texture.height;

//        // UV 좌표 계산
//        float uMin = columnIndex * (spriteWidth / (float)textureWidth);
//        float uMax = (columnIndex + 1) * (spriteWidth / (float)textureWidth);
//        float vMin = 1.0f - (rowIndex + 1) * (spriteHeight / (float)textureHeight);
//        float vMax = 1.0f - rowIndex * (spriteHeight / (float)textureHeight);

//        Mesh mesh = meshFilter.sharedMesh;

//        var uvs = mesh.uv;
//        var vertices = mesh.vertices;

//        for (int i = 0; i < uvs.Length; i++)
//        {
//            float u = Mathf.Lerp(uMin, uMax, vertices[i].x); // X축 기준
//            float v = Mathf.Lerp(vMin, vMax, vertices[i].y); // Y축 기준

//            u = Mathf.Clamp01(u);
//            v = Mathf.Clamp01(v);

//            uvs[i] = new Vector2(u, v);
//        }
//        mesh.uv = uvs;
//}

        // [NavMesh] Key(pivot)
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
            var gx = Mathf.FloorToInt(tilePivot.x / 32);
            var gy = Mathf.FloorToInt(tilePivot.y / 4);
            var gz = Mathf.FloorToInt(tilePivot.z / 32);
            gridPivot = new Vector3(gx, gy, gz);
        }
        private int GetGridKeyMask(Vector3 gridPivot)
        {
            const byte shiftGridXSign = 15;
            const byte shiftGridX = 10;
            const byte shiftGridYSign = 9;
            const byte shiftGridY = 6;
            const byte shiftGridZSign = 5;
            const byte shiftGridZ = 0;

            Vector3Int gridInt = gridPivot.ToInt();

            int gridFlag = 0;

            if (gridInt.x < 0)
            {
                gridFlag |= 1 << shiftGridXSign;
                gridFlag |= (-gridInt.x) << shiftGridX;
            }
            else
            {
                gridFlag |= gridInt.x << shiftGridX;
            }

            if (gridInt.y < 0)
            {
                gridFlag |= 1 << shiftGridYSign;
                gridFlag |= (-gridInt.y) << shiftGridY;
            }
            else
            {
                gridFlag |= gridInt.y << shiftGridY;
            }

            if (gridInt.z < 0)
            {
                gridFlag |= 1 << shiftGridZSign;
                gridFlag |= (-gridInt.z) << shiftGridZ;
            }
            else
            {
                gridFlag |= gridInt.z << shiftGridZ;
            }

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
            // layer 정보도 필요하네
            // const 변수들을 어디서 저장하는게 좋으려나?
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
            ulong newMask = 0;

            var matrix = GetHeightMatrixRotated(rot);
            matrix = RotateMatrix(matrix, rot);

            ulong mask = 0;
            int i = 0;
            for (i = 0; i < 13; ++i)
            {
                switch (i)
                {
                    case 0: mask = (ulong)matrix[0, 4]; break;
                    case 1: mask = (ulong)matrix[2, 4]; break;
                    case 2: mask = (ulong)matrix[4, 4]; break;
                    case 3: mask = (ulong)matrix[1, 3]; break;
                    case 4: mask = (ulong)matrix[3, 3]; break;
                    case 5: mask = (ulong)matrix[0, 2]; break;
                    case 6: mask = (ulong)matrix[2, 2]; break;
                    case 7: mask = (ulong)matrix[4, 2]; break;
                    case 8: mask = (ulong)matrix[1, 1]; break;
                    case 9: mask = (ulong)matrix[3, 1]; break;
                    case 10: mask = (ulong)matrix[0, 0]; break;
                    case 11: mask = (ulong)matrix[2, 0]; break;
                    case 12: mask = (ulong)matrix[4, 0]; break;
                    default: break;
                }

                newMask |= mask << i * 4;
            }

            if (true == isSmall)
            {
                newMask |= 1ul << (4 * i);
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

            return matrix;
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

            Texture texture = meshRenderer.sharedMaterial.mainTexture;
            int textureWidth = texture.width;
            int textureHeight = texture.height;

            // UV 좌표 계산

            // texture 몇 개씩 들어가는지 구하는 프로퍼티나 상수가 필요할 듯? 정리하면 되겠다.
            int columnIndex = (int)textureType % 8;
            int rowIndex = (int)textureType / 8;

            float uMin = columnIndex * (SPRITE_WIDTH / (float)textureWidth);
            float uMax = (columnIndex + 1) * (SPRITE_WIDTH / (float)textureWidth);
            float vMin = 1.0f - (rowIndex + 1) * (SPRITE_HEIGHT / (float)textureHeight);
            float vMax = 1.0f - rowIndex * (SPRITE_HEIGHT / (float)textureHeight);

            Mesh mesh = meshFilter.sharedMesh;

            var uvs = mesh.uv;
            var vertices = mesh.vertices;

            for (int i = 0; i < uvs.Length; i++)
            {
                float u = Mathf.Lerp(uMin, uMax, vertices[i].x); // X축 기준
                float v = Mathf.Lerp(vMin, vMax, vertices[i].y); // Y축 기준

                u = Mathf.Clamp01(u);
                v = Mathf.Clamp01(v);

                uvs[i] = new Vector2(u, v);
            }
            mesh.uv = uvs;
        }

        //[Render] 
        // ...

        #region not_used
        //public async Task BakeMesh(ConcurrentDictionary<uint, ConcurrentDictionary<ulong, MapNavData>> map)
        //{
        //    await Task.Yield();

        //    int rot = (transform.rotation.eulerAngles.y).ToInt();
        //    rot = (rot + 360) % 360;
        //    if (0 != rot % 90)
        //    {
        //        Debug.LogError($"Wrong Rotate: {rot}");
        //        return;
        //    }

        //    // calculate (rotated) pivot
        //    bool isSmall = (naviMask >> (4 * 13)) != 0;
        //    GetPivotRotated(rot, isSmall, out Vector3 gridPivot, out Vector3 tilePivot);

        //    // get key mask

        //    // grid로 그룹 한 번 나누고
        //    ushort gridKeyMask = GetGridKeyMask(gridPivot);
        //    map.TryAdd(gridKeyMask, new ConcurrentDictionary<ulong, MapNavData>());

        //    // grid 안에 tileKeyMask로 분류
        //    uint tileKeyMask = GetTileKeyMask(gridPivot, tilePivot, isSmall);
        //    naviMask = GetNaviMaskRotated(rot, isSmall); // ?? of 64 bits used
        //    infoMask = GetInfoMask();
        //    map[gridKeyMask].TryAdd(tileKeyMask, new MapNavData(naviMask, infoMask));
        //}
        #endregion
    }
}
