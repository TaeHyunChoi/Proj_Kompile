#if UNITY_EDITOR
namespace Kompile.Map.Editor.Data
{
    using Unity.Burst;
    using MessagePack;

// Data: 정보의 상태, 구조, 가공 형태 및 자료형 정의 (Define: Type, Info)
    [BurstCompile]
    [MessagePackObject]
    public struct EditMapTileData
    {
        [Key(0)]
        public long ID;

        [Key(1)]
        public long NaviMask;

        [Key(2)]
        public ushort LinkMask;

        [Key(3)]
        public ushort RenderIndex; // enum 이나 flag가 아니므로 '단일값'이라고 가정함

        [Key(4)]
        public ushort LayerMask;
    }
}
#endif