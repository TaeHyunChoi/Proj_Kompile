namespace Kompile.Unit.Data
{
    using UnityEngine;

    /// <summary>
    /// Brain이 한 프레임에 내리는 모든 의사결정 결과를 담는 값 타입.
    /// Entity.Update()에서 Brain으로부터 수신한 뒤 각 Component에 배분된다.
    /// struct이므로 매 프레임 생성해도 GC 부담 없음.
    /// </summary>
    public struct UnitIntent
    {
        /// <summary> 아무 의도도 없는 기본값. Brain이 없거나 비활성 상태일 때 사용 </summary>
        public static readonly UnitIntent Empty = default;

        /// <summary>
        /// 이번 프레임의 이동 입력 (XZ 방향, 크기 ≤ 1).
        /// x = 좌우(world X), y = 앞뒤(world Z)
        /// MoveComponent와 AnimComponent 모두 소비한다.
        /// </summary>
        public Vector2 MoveInput;

        /// <summary>
        /// 트리거성 애니메이션 명령. None이면 AnimComponent가 무시한다.
        /// Idle / Walk는 MoveInput.magnitude에서 AnimComponent가 스스로 결정하므로 여기에 싣지 않는다.
        /// </summary>
        public UnitAnimCmd AnimCommand;
    }
}
