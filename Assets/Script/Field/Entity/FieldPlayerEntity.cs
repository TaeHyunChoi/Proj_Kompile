namespace Script.Field.Entity
{
    using Script.Field.Data;
    using Script.Unit.Entity;
    using Script.Unit.Data;
    using UnityEngine;

    [RequireComponent(typeof(UnitMoveComponent), typeof(UnitAnimComponent))]
    public class FieldPlayerEntity : UnitEntityBase
    {
        private UnitMoveComponent _moveComponent;
        private UnitAnimComponent _animComponent;

        public override void Initialize(long instanceID, UnitRuntimeContext context)
        {
            InstanceID = instanceID;
            Context = context;

            _moveComponent = transform.GetComponent<UnitMoveComponent>();
            _animComponent = transform.GetComponent<UnitAnimComponent>();

            // mapQuery는 FieldUnitManager가 SetMapQuery()로 나중에 주입
            _moveComponent.Initialize(this, null);
            _animComponent.Initialize(this);
        }

        /// <summary>
        /// FieldUnitManager에서 호출. IMapQueryService를 UnitMoveComponent에 주입합니다.
        /// </summary>
        public void SetMapQuery(IMapQueryService mapQuery)
        {
            _moveComponent.Initialize(this, mapQuery);
        }

        /// <summary>
        /// PlayerControlBrain에서 호출. 이번 프레임의 이동 입력(XZ 방향)을 전달합니다.
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            _moveComponent.SetMoveInput(input);
        }

        public override void ManualUpdate()
        {
            _brain.ManualUpdate();
            _moveComponent.ManualUpdate();
            _animComponent.ManualUpdate();
        }
    }
}
