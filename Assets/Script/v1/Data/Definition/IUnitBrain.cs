namespace Kompile.Entity
{
    using Kompile.Data;

    /// <summary>
    /// 유닛의 행동 패턴을 정의하는 순수 C# 전략 개체.
    /// Update()는 한 프레임의 의사결정 결과를 UnitIntent로 반환한다.
    /// Entity는 이를 수신하여 각 Component에 배분하는 오케스트레이터 역할을 맡는다.
    /// </summary>
    public interface IUnitBrain
    {
        public UnitIntent Calculate();
        public void Clear();
    }
}