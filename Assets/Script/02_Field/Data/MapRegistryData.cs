namespace Kompile.Data
{
    using MessagePack;

    /// <summary>
    /// [Data Layer] 빌드 타임에 생성되는 전체 유효 그리드 키 레지스트리 (Static Table)
    /// </summary>
    [MessagePackObject]
    public class MapRegistryData
    {
        // 에디터에서 정렬되어 저장된 실제 에셋 가 존재해 있는 gridKey 리스트
        [Key(0)] public int[] BakedGridKeys; 
    }
}