using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using static EditMapConsts;

public static class EditMapNaviTileUtil
{
        public static EditMapTileDirFlag GetDirFlag(float x, float z)
        {
            EditMapTileDirFlag flag = EditMapTileDirFlag.NONE;

            if (x > 0) { flag |= EditMapTileDirFlag.RIGHT; }
            else if (x < 0) { flag |= EditMapTileDirFlag.LEFT; }

            if (z > 0) { flag |= EditMapTileDirFlag.UP; }
            else if (z < 0) { flag |= EditMapTileDirFlag.DOWN; }

            return flag;
        }

        public static EditVertexContextData GetVertexIndexInfo(EditMapTileDirFlag flag)
        {
            return flag switch
            {
                EditMapTileDirFlag.LEFT => new EditVertexContextData(5, 0, 10),
                EditMapTileDirFlag.RIGHT => new EditVertexContextData(7, 2, 12),
                EditMapTileDirFlag.UP => new EditVertexContextData(11, 10, 12),
                EditMapTileDirFlag.DOWN => new EditVertexContextData(1, 0, 2),
                _ => default
            };
        }

        public static float3 GetDirectionVector(EditMapTileDirFlag flag)
        {
            return flag switch
            {
                EditMapTileDirFlag.LEFT => new float3(-1f, 0f, 0f),
                EditMapTileDirFlag.RIGHT => new float3(1f, 0f, 0f),
                EditMapTileDirFlag.UP => new float3(0f, 0f, 1f),
                EditMapTileDirFlag.DOWN => new float3(0f, 0f, -1f),
                _ => default
            };
        }

        public static int GetLinkMaskShift(EditMapTileDirFlag flag)
        {
            // 반시계 방향으로 돌린다~!!
            return 2 * flag switch
            {
                EditMapTileDirFlag.LEFT | EditMapTileDirFlag.DOWN => 0,
                EditMapTileDirFlag.DOWN => 1,
                EditMapTileDirFlag.RIGHT | EditMapTileDirFlag.DOWN => 2,
                EditMapTileDirFlag.RIGHT => 3,
                EditMapTileDirFlag.RIGHT | EditMapTileDirFlag.UP => 4,
                EditMapTileDirFlag.UP => 5,
                EditMapTileDirFlag.LEFT | EditMapTileDirFlag.UP => 6,
                EditMapTileDirFlag.LEFT => 7,
                _ => -1
            };
        }

        [BurstCompile]
        public static bool TryGetHeightMask(this in EditMapTileData data, int vertice, out int maskInt)
        {
            const int NONE_SUBTILE = 0b1111;
            
            // data가 'in'이므로 필드 접근 시 복사가 일어나지 않는다.
            int shift = EditMapConsts.HEIGHT_BITS * vertice;
            maskInt = (int)((data.NaviMask >> shift) & EditMapConsts.HEIGHT_MASK);

            return NONE_SUBTILE != maskInt;
        }
}
