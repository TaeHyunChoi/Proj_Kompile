namespace Kompile.Field.Entity
{
    using Kompile.Field.Data;
    using Kompile.Unit.Entity;
    using Kompile.Unit.Component;
    using Kompile.Unit.Data;
    using UnityEngine;
    using static Kompile.Input.Data.Definition;

    [RequireComponent(typeof(UnitMoveComponent), typeof(UnitAnimComponent))]
    public class FieldPlayerEntity : UnitEntityBase
    {
        private UnitMoveComponent _moveComponent;
        private UnitAnimComponent _animComponent;

        /// <summary> mapQuery를 포함한 완전 초기화. AOC 없이 호출되는 경우 (Inspector 할당 또는 AOC 없음). </summary>
        public override void Initialize(UnitRuntimeContext context, IMapQueryService mapQuery)
            => InitCore(context, mapQuery, null);

        /// <summary> AOC를 포함한 완전 초기화. 동적 생성 경로(FieldManager.SpawnPlayerAsync)에서 호출. </summary>
        public void Initialize(UnitRuntimeContext context, IMapQueryService mapQuery, AnimatorOverrideController aoc)
            => InitCore(context, mapQuery, aoc);

        private void InitCore(UnitRuntimeContext context, IMapQueryService mapQuery, AnimatorOverrideController aoc)
        {
            _context = context;
            SetBrain();

            _moveComponent = transform.GetComponent<UnitMoveComponent>();
            _animComponent = transform.GetComponent<UnitAnimComponent>();

            _moveComponent.Initialize(this, mapQuery);
            _animComponent.Initialize(this, aoc);
        }

        public override void UpdateManual(in InputState inputState)
        {
            // Brain이 한 프레임의 의사결정 결과(UnitIntent)를 반환하고,
            // Entity가 이를 각 Component에 배분하는 오케스트레이터 역할을 한다.
            UnitIntent intent = _brain?.Update(in inputState) ?? UnitIntent.Empty;

            _moveComponent.Update_(in intent);
            _animComponent.UpdateIntent(in intent);
        }
    }
}
