#if UNITY_EDITOR
namespace Script.Data
{
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Assertions;
    using static Index.MapTileIndex;

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
            float rot = RotY[index]; //어차피 Mathf.Int 쓰는 듯;
            float3 rotated_position = GetRotatedPivot(Position[index], rot);
            ulong height = Height[index];
            Data[index] = new EditMapTileData()
            {
                GridKey = EditMapUtil.GetGridKeyMask(sceneIndex, rotated_position),
                TileKey = EditMapUtil.GetTileKeyMask(rotated_position),
                NavMask = GetRotatedHeightMask(height, layer, rot),
                LinkMask = 0
            };
        }
        private readonly float3 GetRotatedPivot(float3 position, float rot)
        {
            int rotInt = Mathf.RoundToInt(rot);
            float x = position.x;
            float y = position.y;
            float z = position.z;

            switch (rotInt)
            {
                case 90:  return new float3(x    , y, z - 1);
                case 180: return new float3(x - 1, y, z - 1);
                case 270: return new float3(x - 1, y, z    );
                default: break;
            }
            return position;
        }
        private readonly long GetRotatedHeightMask(ulong heightMask, int navLayer, float rotY)
        {
            int rotInt = Mathf.RoundToInt(rotY);
            rotInt = (rotInt + 360) % 360;

            Assert.IsTrue(rotInt % 90 == 0, $"Tile has Wrong Rotation; ({rotInt})");

            // 여기 값이 작아서 굳이 NativeArray<> 사용하지 않아도 될 것 같은데?
            ulong[,] matrix;
            matrix = BitmaskToMatrix(heightMask);
            matrix = RotateMatrix(matrix, rotInt);
            ulong rotatedHeightMask = MatrixToBitmask(matrix);

            ulong layerMask = (ulong)navLayer << (EditMapUtil.TOTAL_BITS * EditMapUtil.BITS_PER_CELL);
            return (long)(layerMask | rotatedHeightMask);
        }
        private readonly ulong[,] BitmaskToMatrix(ulong mask)
        {
            int size = EditMapUtil.MATRIX_SIZE;
            ulong cellValue;

            ulong[,] matrix = new ulong[size, size];
            int x, y;
            for (int i = 0; i < EditMapUtil.TOTAL_BITS; ++i)
            {
                cellValue = mask & Index.MapTileIndex.HEIGHT_MASK;
                x = EditMapUtil.INDEX_MAP[i].x;
                y = EditMapUtil.INDEX_MAP[i].y;
                matrix[x,y] = cellValue;

                mask >>= EditMapUtil.BITS_PER_CELL;
            }


            return matrix;
        }
        public readonly ulong[,] RotateMatrix(ulong[,] matrix, int rot)
        {
            if (rot == 0)
            {
                return matrix;
            }

            int size = 5;
            ulong[,] rotatedMatrix = new ulong[size, size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    // 현재 버텍스 값
                    ulong vertexValue = matrix[i, j];

                    // 값이 0이 아니면(버텍스가 있으면) 회전 계산
                    if (vertexValue != 0)
                    {
                        int newX = 0;
                        int newY = 0;

                        switch (rot)
                        {
                            case 90:
                                // 90도 회전 (x, y) -> (-y, x) -> (y, size-1-x)
                                newX = size - 1 - j;
                                newY = i;
                                break;
                            case 180:
                                // 180도 회전 (x, y) -> (-x, -y) -> (size-1-x, size-1-y)
                                newX = size - 1 - i;
                                newY = size - 1 - j;
                                break;
                            case 270:
                                // 270도 회전 (x, y) -> (y, -x) -> (j, size-1-i)
                                newX = j;
                                newY = size - 1 - i;
                                break;
                            default:
                                Debug.LogError("잘못된 회전 각도입니다.");
                                return matrix;
                        }
                        rotatedMatrix[newX, newY] = vertexValue;
                    }
                }
            }
            return rotatedMatrix;
        }
        private readonly ulong MatrixToBitmask(ulong[,] matrix)
        {
            ulong newMask = 0ul;
            ulong mask;
            int x, y;

            for (int i = 0; i < EditMapUtil.TOTAL_BITS; ++i)
            {
                x = EditMapUtil.INDEX_MAP[i].x;
                y = EditMapUtil.INDEX_MAP[i].y;
                mask = matrix[x, y];

                newMask |= mask << i * EditMapUtil.BITS_PER_CELL;
            }

            return newMask;
        }
    }
    public struct EditMapTileData
    {
        public int  GridKey;
        public int  TileKey;
        public long NavMask;
        public int  LinkMask;

        public EditMapTileData(EditMapTileData visit_tile, int add_link_mask)
        {
            GridKey  = visit_tile.GridKey;
            TileKey  = visit_tile.TileKey;
            NavMask  = visit_tile.NavMask;
            LinkMask = visit_tile.LinkMask | add_link_mask;
        }
        public readonly float3 GetTilePivot()
        {
            return EditMapUtil.GetTilePosition(GridKey, TileKey);
        }
        public readonly bool TryGetVerticeHeight(int vertice, out int heightx1000)
        {
            long mask = NavMask >> (Index.MapTileIndex.HEIGHT_BITS * vertice);
            int maskInt = (int)mask & Index.MapTileIndex.HEIGHT_MASK;
            if (0b1111 == maskInt)
            {
                heightx1000 = default;
                return false;
            }

            float pivotY = GetTilePivot().y;
            heightx1000 = Mathf.RoundToInt((pivotY + maskInt * 0.125f) * 1000);
            return true;
        }

        public Vector3 TilePosition
        {
            get
            {
                // grid_key to grid_parent_position
                int sign;
                int gx, gy, gz;

                sign = ((GridKey >> SHIFT_GRID_X_SIGN) & 1) == 0 ? 1 : -1;
                gx = sign * ((GridKey >> SHIFT_GRID_X) & GRID_COORD_MASK);

                sign = ((GridKey >> SHIFT_GRID_Y_SIGN) & 1) == 0 ? 1 : -1;
                gy = sign * ((GridKey >> SHIFT_GRID_Y) & GRID_COORD_MASK);

                sign = ((GridKey >> SHIFT_GRID_Z_SIGN) & 1) == 0 ? 1 : -1;
                gz = sign * ((GridKey >> SHIFT_GRID_Z) & GRID_COORD_MASK);

                Vector3 gird_pivot = SIZE_GRID_AXIS * new Vector3(gx, gy, gz);

                // tile_key to tile_child_position
                int tx, ty, tz;

                tx = (TileKey >> SHIFT_TILE_X) & TILE_COORD_MASK;
                ty = (TileKey >> SHIFT_TILE_Y) & TILE_COORD_MASK;
                tz = (TileKey >> SHIFT_TILE_Z) & TILE_COORD_MASK;
                float scale = ((TileKey >> SHIFT_TILE_SMALL) & 1) > 0 ? 0.5f : 1;

                Vector3 tile_pivot = scale * new Vector3(tx, ty, tz);

                return gird_pivot + tile_pivot;
            }
        }
    }
}
#endif