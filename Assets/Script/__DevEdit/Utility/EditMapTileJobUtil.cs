#if UNITY_EDITOR
namespace Kompile.Editor.Utility
{
    using Kompile.Data;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Kompile.Editor.Data;

    /// <summary> 회전 로직을 제거하고, 순수하게 좌표와 HeightMask를 압축하여 NaviMask를 생성 </summary>
    public struct EditMapTileJobUtil : IJobParallelFor
    {
        [ReadOnly] public NativeArray<byte>   SceneIndex;   // 씬 인덱스
        [ReadOnly] public NativeArray<ushort> RenderLayer;  // 레이어 인덱스
        [ReadOnly] public NativeArray<float3> Position;     // 타일 월드 좌표
        [ReadOnly] public NativeArray<ulong>  Height;       // 에디터에서 구워진 13개 버텍스 높이 마스크

        public NativeArray<EditMapTileData> Data;          // 결과 반환용 배열

        public void Execute(int index)
        {
            int    layer      = RenderLayer[index];
            ulong  heightMask = Height[index];
            float3 position   = Position[index];

            ulong layerMask   = (ulong)layer << (MapConsts.TOTAL_BITS * MapConsts.BITS_PER_CELL);
            long  naviMask    = (long)(layerMask | heightMask);

            Data[index] = new EditMapTileData()
            {
                ID          = EditMapCoordUtil.ComputeTileID(position),
                NaviMask    = naviMask,
                LinkMask    = default, // 이웃 타일과의 연결 정보는 다음 Job(LinkJob)에서 계산됩니다.
                RenderIndex = (ushort)layer
            };
        }
    }
}
#endif