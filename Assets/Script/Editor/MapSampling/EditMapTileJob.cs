namespace Script.Data
{
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    public partial struct EditMapTileJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> SceneIndex;  // 씬 인덱스
        [ReadOnly] public NativeArray<int> NavLayer;    // 레이어 인덱스
        [ReadOnly] public NativeArray<float3> Position; // 타일 좌표
        [ReadOnly] public NativeArray<float> RotY;      // 타일 회전값 (y축 회전)
        [ReadOnly] public NativeArray<ulong> Height;    // height mask

        public NativeArray<EditMapTileData> Data;       // result data

        public void Execute(int index)
        {
            int sceneIndex = SceneIndex[index];
            int layer = NavLayer[index];
            float3 position = Position[index];
            float rot = RotY[index];
            ulong height = Height[index];

            Data[index] = new EditMapTileData()
            {
                GridKey = EditMapUtil.GetGridKeyMask(sceneIndex, position, rot),
                TileKey = EditMapUtil.GetTileKeyMask(position),
                NavMask = GetRotatedHeightMask(height, layer, rot)
            };
        }

        private readonly long GetRotatedHeightMask(ulong heightMask, int navLayer, float rotY)
        {
            int rotInt = Mathf.RoundToInt(rotY);
            rotInt = (rotInt + 360) % 360;
            if (rotInt % 90 != 0)
            {
                Debug.LogError($"Tile has Wrong Rotation; ({rotInt})");
                return 0;
            }

            NativeArray<ulong> matrix;
            matrix = BitmaskToMatrix(heightMask);
            matrix = RotateMatrix(matrix, rotInt);
            ulong rotatedHeightMask = MatrixToBitmask(matrix);

            ulong layerMask = (ulong)navLayer << (EditMapUtil.TOTAL_BITS * EditMapUtil.BITS_PER_CELL);
            return (long)(layerMask | rotatedHeightMask);
        }
        private readonly NativeArray<ulong> BitmaskToMatrix(ulong mask)
        {
            int size = EditMapUtil.MATRIX_SIZE;
            ulong cellValue;
            int index;

            NativeArray<ulong> matrix = new NativeArray<ulong>(size * size, Allocator.Temp);

            for (int i = 0; i < EditMapUtil.TOTAL_BITS; ++i)
            {
                cellValue = mask & Script.Index.MapTileIndex.HEIGHT_MASK;
                index = (EditMapUtil.INDEX_MAP[i].x * size) + EditMapUtil.INDEX_MAP[i].y;
                matrix[index] = cellValue;

                mask >>= EditMapUtil.BITS_PER_CELL;
            }

            return matrix;
        }
        private readonly NativeArray<ulong> RotateMatrix(NativeArray<ulong> matrix, int rot)
        {
            if (0 == rot)
            {
                return matrix;
            }

            int size = EditMapUtil.MATRIX_SIZE;
            NativeArray<ulong> rotatedMatrix = new NativeArray<ulong>(size * size, Allocator.Temp);

            int index;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    switch (rot)
                    {
                        case 90: index = (j * size) + (size - 1 - i); break;
                        case 180: index = ((size - 1) * size) + (size - 1 - j); break;
                        case 270: index = ((size - 1 - j) * size) + i; break;
                        default:
                            UnityEngine.Debug.Assert(false, "wrong rotation");
                            return matrix;
                    }

                    rotatedMatrix[index] = matrix[i * size + j];
                }
            }
            return rotatedMatrix;
        }
        private readonly ulong MatrixToBitmask(NativeArray<ulong> matrix)
        {
            ulong newMask = 0ul;
            ulong mask;

            int size = EditMapUtil.MATRIX_SIZE;
            int index;

            for (int i = 0; i < EditMapUtil.TOTAL_BITS; ++i)
            {
                index = (EditMapUtil.INDEX_MAP[i].x * size) + EditMapUtil.INDEX_MAP[i].y;
                mask = matrix[index];
                newMask |= mask << i * EditMapUtil.BITS_PER_CELL;
            }

            return newMask;
        }
    }
    public struct EditMapTileData
    {
        [ReadOnly] public int GridKey;
        [ReadOnly] public int TileKey;
        [ReadOnly] public long NavMask;

        public float3 Pivot
        {
            get
            {

                return new float3(0, 0, 0);
            }
        }
    }
}