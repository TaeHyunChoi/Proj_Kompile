namespace Script.Data
{
    using static Script.Map.Data.MapConsts;
    using Script.Map.Utility;
    using Unity.Burst;
    using UnityEngine;

    [BurstCompile]
    public struct EditMapTileData
    {
        public long ID;
        public long NaviMask;
        public ushort LinkMask;
        public ushort RenderIndex; // enum 이나 flag가 아니므로 '단일값'이라고 가정함

        public readonly bool TryGetVerticeHeight(int vertice, out int heightx1000)
        {

            long mask = NaviMask >> (HEIGHT_BITS * vertice);
            int maskInt = (int)mask & HEIGHT_MASK;
            if (0b1111 == maskInt)
            {
                heightx1000 = default;
                return false;
            }

            float pivotY = MapCoordUtil.ComputeWorldPosition(ID).y;
            heightx1000 = Mathf.RoundToInt((pivotY + maskInt * 0.125f) * 1000);
            return true;
        }
    }
}