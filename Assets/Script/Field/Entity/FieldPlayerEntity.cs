namespace Kompile.Field.Entity
{
    using Kompile.Field.Data;
    using Kompile.Unit.Entity;
    using Kompile.Unit.Component;
    using Kompile.Unit.Data;
    using UnityEngine;

    [RequireComponent(typeof(UnitMoveComponent), typeof(UnitAnimComponent))]
    public class FieldPlayerEntity : UnitEntityBase, IMovable
    {
        private UnitMoveComponent _moveComponent;
        private UnitAnimComponent _animComponent;

        public override void Initialize(long instanceID, UnitRuntimeContext context)
        {
            _instanceID = instanceID;
            _context = context;
            SetBrain();

            _moveComponent = transform.GetComponent<UnitMoveComponent>();
            _animComponent = transform.GetComponent<UnitAnimComponent>();

            // mapQuery는 FieldUnitManager가 SetMapQuery()로 나중에 주입
            _moveComponent.Initialize(this, null);
            _animComponent.Initialize(this);
        }

        public override void Update()
        {
            // Brain이 한 프레임의 의사결정 결과(UnitIntent)를 반환하고,
            // Entity가 이를 각 Component에 배분하는 오케스트레이터 역할을 한다.
            UnitIntent intent = _brain != null ? _brain.Update() : UnitIntent.Empty;

            _moveComponent.Update_(in intent);
            _animComponent.Update_(in intent);
        }

        /// <summary> FieldUnitManager에서 호출. IMapQueryService를 UnitMoveComponent에 주입 </summary>
        public void SetMapQuery(IMapQueryService mapQuery)
        {
            _moveComponent.Initialize(this, mapQuery);
        }

        /// <summary>
        /// IMovable 구현. Brain을 거치지 않고 외부 시스템(컷씬, 넉백 등)이
        /// 직접 이동 입력을 밀어넣어야 할 때 사용한다.
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            UnitIntent intent = new UnitIntent { MoveInput = input };
            _moveComponent.Update_(in intent);
        }
    }
}
