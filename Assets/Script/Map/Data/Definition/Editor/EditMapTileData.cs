#if UNITY_EDITOR
// [Framework] 규격에 따라 단수형 Script.Map.Data를 Scripts.Datas.Map으로 변경
namespace Script.Map.Data
{
    using Unity.Burst;
    using MessagePack;

    // Data: 정보의 상태, 구조, 가공 형태 및 자료형 정의 (Define: Type, Info)
    [BurstCompile]
    [MessagePackObject] // MsgPack003 오류 해결: 직렬화 대상임을 명시
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
    }
}
#endif