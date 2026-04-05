namespace Script.Field.Entity
{
    using UnityEngine;
    using Script.Field.Data;      // Data 계층 (Context, Model)

    /// <summary> Field 상의 유닛 개체. 하위 Component들을 묶어 논리적인 실체를 구성 </summary>
    public class FieldPlayerEntity : FieldUnitEntityBase
    {
        // --- Components ---
        private UnitMoveComponent _moveComponent;
        private UnitAnimComponent _animComponent;

        private void Awake()
        {
            _moveComponent = transform.GetComponent<UnitMoveComponent>();
            if (true == _moveComponent)
            {
                _moveComponent.Initialize(this);
            }

            _animComponent = transform.GetComponent<UnitAnimComponent>();
            if (true == _animComponent)
            {
                _animComponent.Initialize(this);
            }
        }

        public override void ManualUpdate()
        {
            // child component
            // ...
        }
    }
}