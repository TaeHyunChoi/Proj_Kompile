namespace Kompile.Data
{
    /// <summary>
    /// Brain이 한 프레임에 AnimComponent에 전달하는 트리거성 애니메이션 명령.
    /// 루프 상태(Idle, Walk)는 UnitIntent.MoveInput 크기에서 AnimComponent가 스스로 판단하므로 여기에 포함하지 않는다.
    /// </summary>
    public enum UnitAnimCmd
    {
        None = 0,
        Idle,
        Walk,
        Attack,
        Hit,
        Dead,
    }
}
