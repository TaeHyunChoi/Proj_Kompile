namespace Kompile.Data
{
    /// <summary> 마스터 AOC 템플릿과 런타임 클립 배열 간의 고유 인덱스 정의 </summary>
    public enum FieldUnitAnimIndex
    {
        Idle = 0,
        Walk = 1,
        // 필요 시 추가 (ex. Run, Sleep...)
        Count
    }

    /// <summary> 2.5D 8방향 블렌드 트리에 완벽히 대응하는 클립 인덱스 규칙 </summary>
    public enum EUnitAnimIndex : byte
    {
        // 8방향 Idle
        Idle_N = 0,
        Idle_NE,
        Idle_E,
        Idle_SE, 
        Idle_S, 
        Idle_SW, 
        Idle_W,
        Idle_NW,

        // 8방향 Walk
        Walk_N,
        Walk_NE, 
        Walk_E, 
        Walk_SE, 
        Walk_S, 
        Walk_SW, 
        Walk_W, 
        Walk_NW,

        Count // 총 16개 클립 규칙화
    }
}