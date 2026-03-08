namespace Script.Map.Data
{
    using Unity.Burst;

    [BurstCompile]
    public struct EditMapTileData
    {
        public long ID;
        public long NaviMask;
        public ushort LinkMask;
        public ushort RenderIndex; // enum 이나 flag가 아니므로 '단일값'이라고 가정함
    }
}