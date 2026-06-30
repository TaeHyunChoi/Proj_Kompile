namespace Kompile.Unit.Entity
{
    using UnityEngine;

    /// <summary>
    /// 이동 입력을 수신할 수 있는 유닛 Entity가 구현하는 인터페이스.
    /// Brain이 구체 Entity 타입에 의존하지 않고 SetMoveInput()을 호출할 수 있도록 분리.
    /// 이동이 필요 없는 Entity(고정 포탑 등)는 구현하지 않는다.
    /// </summary>
    public interface IMovable
    {
        /// <summary>
        /// 이번 프레임의 이동 입력(XZ 방향)을 전달합니다.
        /// x = 좌우(world X), y = 앞뒤(world Z)
        /// </summary>
        void SetMoveInput(Vector2 input);
    }
}
