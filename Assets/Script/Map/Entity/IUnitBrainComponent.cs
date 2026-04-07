namespace Script.Field.Entity
{
    /// <summary> 
    /// 유닛의 의사결정(키보드 조작, AI 상태머신 등)을 전담하는 컴포넌트 인터페이스.
    /// 명칭은 I... 형식을 사용합니다.
    /// </summary>
    public interface IUnitBrainComponent
    {
        void Initialize(FieldUnitEntity ownerEntity);
        void ManualUpdate();
    }
}